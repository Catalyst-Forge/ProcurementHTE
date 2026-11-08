using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

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

        public static Header From(object?[]? groupRow, object?[] nameRow)
        {
            var header = new Header();
            string? group = null;
            for (var j = FirstColumn; j <= LastColumn; j++)
            {
                // Left of FirstGroupColumn the group row holds column totals, not group names.
                if (j >= FirstGroupColumn && groupRow?[j] is string groupText && !string.IsNullOrWhiteSpace(groupText))
                    group = groupText.Trim();
                var name = Text(nameRow[j]);
                var ownName = name.Length > 0 ? name : null;
                if (ownName is null && group is null)
                    continue;
                header._columns[j] = (group, ownName, LdpArchiveColumns.LabelFor(group, ownName));
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
            return header;
        }

        /// <summary>The row of "Column1, Column2…" names an Excel table leaves under the header.</summary>
        public bool IsPlaceholder(object?[] cells) =>
            Text(cells[_typed["VendorName"]]).StartsWith("Column", StringComparison.OrdinalIgnoreCase);

        public bool BelongsInArchive(object?[] cells)
        {
            var done = new[] { "NoPo", "NoSa", "NoSp3" }.All(column => LdpArchiveColumns.IsFilled(Text(cells[_typed[column]])));
            return done
                || cells[_typed["StartDate"]] is DateTime start
                    && start.Date >= LdpArchiveColumns.PresentedFrom
                    && start.Date <= LdpArchiveColumns.PresentedTo;
        }

        public LdpArchiveRow ToRow(object?[] cells, int rowNumber)
        {
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
