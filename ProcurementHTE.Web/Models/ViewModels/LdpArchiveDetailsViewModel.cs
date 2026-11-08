using System.Globalization;
using System.Text.Json;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels;

public class LdpArchiveDetailsViewModel
{
    public required LdpArchiveRow Row { get; init; }
    public required IReadOnlyList<LdpArchiveValue> Values { get; init; }
}

/// <summary>One spreadsheet cell, as the import script wrote it: a label, a type, and a value.</summary>
public sealed class LdpArchiveValue
{
    private static readonly CultureInfo Indonesian = new("id-ID");

    public string Label { get; init; } = default!;
    public string Type { get; init; } = "text";
    public JsonElement Value { get; init; }

    public string Display =>
        Type switch
        {
            "number" when Value.ValueKind == JsonValueKind.Number => Value.GetDecimal().ToString(
                Value.GetDecimal() % 1 == 0 ? "N0" : "N2",
                Indonesian
            ),
            "date" when DateTime.TryParse(Value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) =>
                date.ToString(date.TimeOfDay == TimeSpan.Zero ? "dd MMM yyyy" : "dd MMM yyyy HH:mm", Indonesian),
            _ => Value.ValueKind == JsonValueKind.String ? Value.GetString() ?? string.Empty : Value.ToString(),
        };
}
