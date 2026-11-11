using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;

namespace ProcurementHTE.Infrastructure.Services;

public sealed partial class LdpArchiveImporter
{
    internal static readonly PropertyInfo[] TypedProperties = LdpArchiveColumns.Typed
        .Select(column => typeof(LdpArchiveRow).GetProperty(column.Property)!)
        .ToArray();

    internal sealed record Cell(string Label, string Type, object Value);

    /// <summary>
    /// The two header rows: groups such as "PR" or "Aprvl MGR" sit above names such as
    /// "Mulai", so a repeated name is told apart by its group.
    /// </summary>
    private sealed class Header
    {
        private readonly SortedDictionary<int, (string? Group, string? Name, string Label)> _columns = [];
        private readonly Dictionary<string, int> _typed = [];
        private int? _managerStart;
        private int? _managerEnd;
        private int? _prRelease;
        private int? _inpro;
        private int? _hardCopy;

        public static Header From(object?[]? groupRow, object?[] nameRow)
        {
            var header = new Header();
            string? group = null;
            var used = new HashSet<string>();
            var invoiceStarted = false;
            for (var j = FirstColumn; j <= LastColumn; j++)
            {
                // Left of FirstGroupColumn the group row holds column totals, not group names.
                if (j >= FirstGroupColumn && groupRow?[j] is string groupText && !string.IsNullOrWhiteSpace(groupText))
                    group = groupText.Trim();
                var name = Text(nameRow[j]);
                var ownName = name.Length > 0 ? name : null;
                if (ownName is null && group is null)
                    continue;

                // The invoice stage repeats the PR stage's approval columns. Its copies get a
                // suffix, so every label stays unique in the detail view, the edit form, and exports.
                invoiceStarted |= group == LdpArchiveColumns.InvoiceGroup;
                var label = LdpArchiveColumns.LabelFor(group, ownName);
                if (!used.Add(label))
                {
                    var suffix = invoiceStarted ? " (Invoice)" : " (2)";
                    for (var copy = 2; !used.Add(label + suffix); copy++)
                        suffix = $" ({copy + 1})";
                    label += suffix;
                }
                header._columns[j] = (group, ownName, label);
            }

            var missing = new List<string>();
            foreach (var column in LdpArchiveColumns.Typed)
            {
                var found = header._columns.FirstOrDefault(pair =>
                    pair.Value.Name == column.Name && pair.Value.Group == column.Group
                );
                if (found.Value.Label is null)
                    missing.Add(column.Label);
                else
                    header._typed[column.Property] = found.Key;
            }

            if (missing.Count > 0)
                throw new LdpImportException($"Kolom tidak ditemukan: {string.Join(", ", missing)}.");

            header._managerStart = header.ColumnOf(LdpArchiveColumns.ManagerApprovalStart);
            header._managerEnd = header.ColumnOf(LdpArchiveColumns.ManagerApprovalEnd);
            header._prRelease = header.ColumnOf(LdpArchiveColumns.PrRelease);
            header._inpro = header.ColumnOf(LdpArchiveColumns.SubmitInpro);
            header._hardCopy = header.ColumnOf(LdpArchiveColumns.SubmitHardCopy);
            return header;
        }

        private int? ColumnOf(string label) =>
            _columns.Where(pair => pair.Value.Label == label).Select(pair => (int?)pair.Key).FirstOrDefault();

        /// <summary>The row of "Column1, Column2…" names an Excel table leaves under the header.</summary>
        public bool IsPlaceholder(object?[] cells) =>
            Text(cells[_typed["VendorName"]]).StartsWith("Column", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// A job with a PO number that either finished (No SA and No SP3 filled too) or started within
        /// the LDP range. A job without a PO number stays out: it is run in Docutrax instead.
        /// </summary>
        public bool BelongsInArchive(object?[] cells, LdpArchiveRange range)
        {
            bool Filled(string column) => LdpArchiveColumns.IsFilled(Text(cells[_typed[column]]));
            return Filled("NoPo") && ((Filled("NoSa") && Filled("NoSp3")) || InRange(cells, range));
        }

        private bool InRange(object?[] cells, LdpArchiveRange range) => range.Contains(cells[_typed["StartDate"]] as DateTime?);

        public LdpArchiveRow ToRow(object?[] cells, int rowNumber, LdpArchiveRange range)
        {
            LdpProcessDates? schedule = null;
            if (InRange(cells, range) && cells[_typed["StartDate"]] is DateTime start)
            {
                schedule = LdpProcessSchedule.For(start, _hardCopy is { } hardCopy ? AsDate(cells[hardCopy]) : null);
                cells = WithSchedule(cells, schedule);
            }

            var row = new LdpArchiveRow { SourceRow = rowNumber, Status = LdpArchiveColumns.CompletedStatus, ImportBatch = string.Empty };
            foreach (var column in LdpArchiveColumns.Typed)
                SetTyped(row, column, cells[_typed[column.Property]]);

            var values = _columns
                .Where(pair => cells[pair.Key] is not null && Text(cells[pair.Key]) is { Length: > 0 } text && text != "-")
                .Select(pair => ToCell(pair.Value.Label, cells[pair.Key]!))
                .ToList();
            if (schedule is not null)
                AddApprovals(values, schedule);
            row.ValuesJson = JsonSerializer.Serialize(values, JsonOptions);
            return row;
        }

        /// <summary>
        /// A job in the LDP went from its start to Submit INPRO within
        /// <see cref="LdpProcessSchedule.MaxDaysToInpro"/> days (<see cref="LdpProcessSchedule"/>). The rule
        /// sets the cells before they are stored, so importing again keeps the same values. The invoice
        /// stage's approval columns are left as recorded.
        /// </summary>
        private object?[] WithSchedule(object?[] cells, LdpProcessDates schedule)
        {
            var copy = (object?[])cells.Clone();
            void Set(int? column, DateTime date)
            {
                if (column is { } index)
                    copy[index] = date;
            }

            Set(_typed["PrCreatedAt"], schedule.PrCreated);
            Set(_prRelease, schedule.PrReleased);
            Set(_managerStart, schedule.ManagerApproved);
            Set(_managerEnd, schedule.ManagerApproved);
            Set(_inpro, schedule.InproSubmitted);
            Set(_hardCopy, schedule.HardCopySubmitted);
            return copy;
        }

        // The spreadsheet has no analyst or assistant manager approval: their cells go just before
        // the manager's, where the approvals read in order.
        private static void AddApprovals(List<Cell> values, LdpProcessDates schedule)
        {
            var at = values.FindIndex(cell => cell.Label == LdpArchiveColumns.ManagerApprovalStart);
            if (at < 0)
                at = values.FindIndex(cell => cell.Label == LdpArchiveColumns.ManagerApprovalEnd);
            if (at < 0)
                at = values.Count;
            values.InsertRange(at,
            [
                ToCell(LdpArchiveColumns.AnalystApprovalEnd, schedule.AnalystApproved),
                ToCell(LdpArchiveColumns.AsstManagerApprovalEnd, schedule.AsstManagerApproved),
            ]);
        }

        private static DateTime? AsDate(object? value) => value is DateTime { Year: > 1900 } date ? date : null;
    }

    private static void SetTyped(LdpArchiveRow row, LdpArchiveColumns.Column column, object? value)
    {
        var property = typeof(LdpArchiveRow).GetProperty(column.Property)!;
        object? typed = column.Kind switch
        {
            "date" => value is DateTime date && date.Year > 1900 ? date : null,
            "number" => value is double number ? Math.Round((decimal)number, 2) : null,
            _ => TextFor(column, value, property),
        };
        property.SetValue(row, typed);
    }

    private static string? TextFor(LdpArchiveColumns.Column column, object? value, PropertyInfo property)
    {
        var text = Text(value);
        if (text.Length == 0 || text == "-")
            return null;
        if (column.Property == "JobType" && LdpArchiveColumns.JobTypes.TryGetValue(text, out var jobType))
            text = jobType;
        var length = property.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ?? 1000;
        return Clip(text, length);
    }

    private static Cell ToCell(string label, object value) =>
        value switch
        {
            DateTime { Year: <= 1900 } time => new Cell(label, "text", time.ToString("HH:mm", CultureInfo.InvariantCulture)),
            DateTime date => new Cell(label, "date", date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
            double number => new Cell(label, "number", Math.Round((decimal)number, 2)),
            bool flag => new Cell(label, "text", flag ? "Ya" : "Tidak"),
            _ => new Cell(label, "text", Text(value)),
        };
}
