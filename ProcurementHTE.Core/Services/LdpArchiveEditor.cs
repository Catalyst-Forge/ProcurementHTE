using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

/// <summary>
/// Applies an Admin's edit to an archive row. The typed columns and ValuesJson describe the
/// same cells, so a typed field is written to both and the detail view never disagrees.
/// </summary>
public static class LdpArchiveEditor
{
    private static readonly CultureInfo Indonesian = new("id-ID");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static void Apply(
        LdpArchiveRow row,
        string status,
        IReadOnlyDictionary<string, string?> typed,
        IReadOnlyDictionary<string, string?> others
    )
    {
        row.Status = string.IsNullOrWhiteSpace(status) ? LdpArchiveColumns.CompletedStatus : status.Trim();
        var cells = Read(row.ValuesJson);

        foreach (var column in LdpArchiveColumns.Typed)
        {
            var input = typed.TryGetValue(column.Property, out var text) ? text?.Trim() : null;
            var value = Parse(column.Kind, input);
            typeof(LdpArchiveRow).GetProperty(column.Property)!.SetValue(row, value);
            Put(cells, column.Label, column.Kind, value);
        }

        foreach (var (label, text) in others)
        {
            var existing = cells.FirstOrDefault(cell => cell.Label == label);
            var kind = existing?.Type ?? "text";
            var value = Parse(kind, text?.Trim()) ?? (string.IsNullOrWhiteSpace(text) ? null : text.Trim());
            Put(cells, label, value is string ? "text" : kind, value);
        }

        row.ValuesJson = JsonSerializer.Serialize(cells.Select(cell => new { cell.Label, cell.Type, cell.Value }), JsonOptions);
    }

    public sealed class Cell
    {
        public string Label { get; set; } = default!;
        public string Type { get; set; } = "text";
        public JsonNode? Value { get; set; }
    }

    public static List<Cell> Read(string? json) =>
        string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<Cell>>(json, JsonOptions) ?? [];

    private static void Put(List<Cell> cells, string label, string kind, object? value)
    {
        var index = cells.FindIndex(cell => cell.Label == label);
        if (value is null)
        {
            if (index >= 0)
                cells.RemoveAt(index);
            return;
        }

        var node = value switch
        {
            DateTime date => JsonValue.Create(date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
            decimal number => JsonValue.Create(number),
            _ => JsonValue.Create(value.ToString()),
        };
        var cell = new Cell { Label = label, Type = kind, Value = node };
        if (index >= 0)
            cells[index] = cell;
        else
            cells.Add(cell);
    }

    /// <summary>False when the text is not empty but cannot be read as the column's kind.</summary>
    public static bool IsValid(string kind, string? text) =>
        string.IsNullOrWhiteSpace(text) || kind == "text" || Parse(kind, text.Trim()) is not null;

    private static object? Parse(string kind, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return kind switch
        {
            "date" => DateTime.TryParseExact(text, ["yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "dd/MM/yyyy", "dd MMM yyyy"], Indonesian, DateTimeStyles.None, out var date)
                || DateTime.TryParseExact(text, ["dd MMM yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                ? date
                : null,
            "number" => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
                || decimal.TryParse(text, NumberStyles.Number, Indonesian, out number)
                ? number
                : null,
            _ => text,
        };
    }
}
