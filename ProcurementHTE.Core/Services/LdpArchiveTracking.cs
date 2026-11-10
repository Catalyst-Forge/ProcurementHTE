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
    public static IReadOnlyList<LdpTrackingStep> Steps(LdpArchiveRow row)
    {
        var cells = LdpArchiveEditor.Read(row.ValuesJson)
            .GroupBy(cell => cell.Label)
            .ToDictionary(group => group.Key, group => group.First().Value?.ToString());

        DateTime? Date(string label) =>
            DateTime.TryParse(cells.GetValueOrDefault(label), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;

        return
        [
            new("PR dibuat", row.PrCreatedAt),
            new("PR rilis", Date("PR · Release")),
            new("Approval Manager", Date(LdpArchiveColumns.ManagerApprovalEnd)),
            new("Approval VP", Date("Aprvl VP · Selesai")),
            new("Submit INPRO", Date("Submit INPRO")),
            new("Submit Hard Copy", Date("Submit Hardcopy SCM")),
            new("PO", row.PoDate),
            new("Invoice diterima", Date("Tgl Process Inv Mitra · Terima")),
            new("SA-SP3", Date("Tgl Process Inv Mitra · SA-SP3")),
        ];
    }
}
