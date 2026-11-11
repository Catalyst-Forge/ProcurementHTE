using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;

namespace ProcurementHTE.Web.Models.ViewModels;

public class LdpArchiveFormModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Status wajib diisi.")]
    [StringLength(50)]
    public string Status { get; set; } = LdpArchiveColumns.CompletedStatus;

    /// <summary>The typed columns, keyed by property name.</summary>
    public Dictionary<string, string?> Typed { get; set; } = [];

    /// <summary>Every other spreadsheet column, shown for an edit.</summary>
    public List<LdpArchiveOtherField> Others { get; set; } = [];

    /// <summary>
    /// The tracking dates kept in spreadsheet cells, by label (LdpArchiveTracking.Fields). The two
    /// kept in typed columns, PR dibuat and PO, are posted with <see cref="Typed"/>.
    /// </summary>
    public List<LdpArchiveOtherField> TrackingDates { get; set; } = [];

    public string? TrackingDate(string label) => TrackingDates.FirstOrDefault(field => field.Label == label)?.Value;

    public static LdpArchiveFormModel From(LdpArchiveRow row)
    {
        var typedLabels = LdpArchiveColumns.Typed.Select(column => column.Label).ToHashSet();
        var trackingLabels = LdpArchiveTracking.Fields.Where(field => field.Label is not null).Select(field => field.Label!).ToHashSet();
        var cells = LdpArchiveEditor.Read(row.ValuesJson)
            .Select(cell => new LdpArchiveOtherField
            {
                Label = cell.Label,
                Value = cell.Type == "date" && DateTime.TryParse(cell.Value?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : cell.Value?.ToString(),
            })
            .ToList();
        return new LdpArchiveFormModel
        {
            Id = row.LdpArchiveRowId,
            Status = row.Status,
            Typed = LdpArchiveColumns.Typed.ToDictionary(
                column => column.Property,
                column => Format(typeof(LdpArchiveRow).GetProperty(column.Property)!.GetValue(row))
            ),
            Others = cells.Where(cell => !typedLabels.Contains(cell.Label) && !trackingLabels.Contains(cell.Label)).ToList(),
            TrackingDates = cells.Where(cell => trackingLabels.Contains(cell.Label)).ToList(),
        };
    }

    private static string? Format(object? value) =>
        value switch
        {
            DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal number => number.ToString("0.##", CultureInfo.InvariantCulture),
            _ => value?.ToString(),
        };
}

public class LdpArchiveOtherField
{
    public string Label { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class LdpImportPageViewModel
{
    public string? Token { get; init; }
    public string? FileName { get; init; }
    public LdpImportSummary? Summary { get; init; }

    /// <summary>The LDP range in force, or the one the previewed file was read with.</summary>
    public LdpArchiveRange Range { get; init; } = LdpArchiveRange.Default;
}
