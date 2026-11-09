using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

/// <summary>
/// Plans an import against the jobs the list holds now, and saves it one cell at a time through
/// the same call that saves a cell filled in the table, so the same rules and stage changes apply.
/// The finance tables hold the published jobs; the LDP holds every Docutrax job (not its Arsip rows).
/// </summary>
public sealed class FinanceImportService(
    IProcurementRepository procurements,
    ILdpService ldp,
    IProcurementTrackingService tracking
) : IFinanceImportService
{
    public async Task<FinanceImportPlan> PreviewAsync(FinanceImportTarget target, ExcelSheet sheet, CancellationToken ct = default)
    {
        IEnumerable<FinanceImportJob> jobs = target == FinanceImportTarget.Ldp
            ? (await ldp.GetAllForExportAsync(null, ct)).Where(item => item.ArchiveRowId is null).Select(FinanceImport.FromLdp)
            : (await procurements.GetPublishedForFinanceAsync(ct)).Select(FinanceTable.ToRow).Select(FinanceImport.FromRow);

        var byId = jobs
            .GroupBy(job => job.ProcurementId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        return FinanceImport.Plan(target, sheet, byId);
    }

    public async Task<FinanceImportResult> ApplyAsync(FinanceImportPlan plan, string userId, CancellationToken ct = default)
    {
        var saved = 0;
        var failed = new List<(FinanceImportChange Change, string Reason)>();
        foreach (var change in plan.Changes)
        {
            var result = await tracking.UpdateFinanceFieldAsync(change.ProcurementId, change.Field, change.Value, userId, ct);
            if (result.Success)
                saved++;
            else
                failed.Add((change, result.Error ?? "Tidak tersimpan."));
        }

        return new FinanceImportResult(saved, failed);
    }
}
