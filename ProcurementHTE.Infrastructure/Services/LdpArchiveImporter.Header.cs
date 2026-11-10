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
            if (InRange(cells, range))
                cells = WithManagerApprovalOnTime(cells);

            var row = new LdpArchiveRow { SourceRow = rowNumber, Status = LdpArchiveColumns.CompletedStatus, ImportBatch = string.Empty };
            foreach (var column in LdpArchiveColumns.Typed)
                SetTyped(row, column, cells[_typed[column.Property]]);

            var values = _columns
                .Where(pair => cells[pair.Key] is not null && Text(cells[pair.Key]) is { Length: > 0 } text && text != "-")
                .Select(pair => ToCell(pair.Value.Label, cells[pair.Key]!))
                .ToList();
            row.ValuesJson = JsonSerializer.Serialize(values, JsonOptions);
            return row;
        }

        /// <summary>
        /// A job in the LDP passed the manager's approval within <see cref="LdpManagerApproval.MaxDaysAfterStart"/>
        /// days of its start. The rule sets the cell before it is stored, so importing again keeps the same value.
        /// </summary>
        private object?[] WithManagerApprovalOnTime(object?[] cells)
        {
            if (_managerEnd is not { } end || cells[_typed["StartDate"]] is not DateTime start)
                return cells;

            var copy = (object?[])cells.Clone();
            var finished = LdpManagerApproval.Finished(start, AsDate(copy[end]));
            copy[end] = finished;
            if (_managerStart is { } begin && LdpManagerApproval.Started(AsDate(copy[begin]), finished) is { } started)
                copy[begin] = started;
            return copy;
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
