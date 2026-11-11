using System.Globalization;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

/// <summary>
/// The tracking of an archived job, built from the dates the old LDP spreadsheet recorded. The
/// stages follow a Docutrax job's: PR, approvals, INPRO, hard copy, PO, then the invoice. A stage
/// the spreadsheet has no date for stays open.
/// </summary>
public static class LdpArchiveTracking
{
    /// <summary>
    /// One stage and where its date lives: a typed column (<see cref="Property"/>) or a spreadsheet
    /// cell (<see cref="Label"/>). An <see cref="Optional"/> stage has no spreadsheet column, so it
    /// shows only once the import or an Admin gave it a date.
    /// </summary>
    public sealed record Field(string Step, string? Label = null, string? Property = null, bool Optional = false);

    /// <summary>The stages in order. The edit form offers the same list, so an Admin edits what the tracking shows.</summary>
    public static readonly IReadOnlyList<Field> Fields =
    [
        new("PR dibuat", Property: nameof(LdpArchiveRow.PrCreatedAt)),
        new("PR rilis", Label: LdpArchiveColumns.PrRelease),
        new("Approval Analyst", Label: LdpArchiveColumns.AnalystApprovalEnd, Optional: true),
        new("Approval Asst. Manager", Label: LdpArchiveColumns.AsstManagerApprovalEnd, Optional: true),
        new("Approval Manager", Label: LdpArchiveColumns.ManagerApprovalEnd),
        new("Approval VP", Label: LdpArchiveColumns.VpApprovalEnd),
        new("Submit INPRO", Label: LdpArchiveColumns.SubmitInpro),
        new("Submit Hard Copy", Label: LdpArchiveColumns.SubmitHardCopy),
        new("PO", Property: nameof(LdpArchiveRow.PoDate)),
        new("Invoice diterima", Label: "Tgl Process Inv Mitra · Terima"),
        new("SA-SP3", Label: "Tgl Process Inv Mitra · SA-SP3"),
    ];

    public static IReadOnlyList<LdpTrackingStep> Steps(LdpArchiveRow row)
    {
        var cells = LdpArchiveEditor.Read(row.ValuesJson)
            .GroupBy(cell => cell.Label)
            .ToDictionary(group => group.Key, group => group.First().Value?.ToString());

        DateTime? Date(Field field)
        {
            if (field.Property is { } property)
                return typeof(LdpArchiveRow).GetProperty(property)!.GetValue(row) as DateTime?;
            return DateTime.TryParse(cells.GetValueOrDefault(field.Label!), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;
        }

        return Fields
            .Select(field => (field, date: Date(field)))
            .Where(step => !step.field.Optional || step.date is not null)
            .Select(step => new LdpTrackingStep(step.field.Step, step.date))
            .ToList();
    }
}
