using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Web.Extensions;

namespace ProcurementHTE.Web.Controllers.Finance;

/// <summary>What the AP-PO, AP-Invoice, and AR controllers share: the table page and the cell save.</summary>
public static class FinanceTableEndpoints
{
    public static readonly int[] PageSizes = [10, 25, 50, 100];

    public static Task<Core.Common.PagedResult<FinanceTableRow>> LoadAsync(
        IProcurementQueryService queryService,
        HttpRequest request,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct
    ) =>
        queryService.GetFinanceTableAsync(
            search,
            request.ReadListFilters(),
            request.ReadListSort(),
            page,
            PageSizes.Contains(pageSize) ? pageSize : 25,
            ct
        );

    /// <summary>
    /// Saves one cell and answers with what the cell and the row status should now show,
    /// so the table updates in place.
    /// </summary>
    public static async Task<IActionResult> SaveFieldAsync(
        IProcurementTrackingService trackingService,
        IProcurementQueryService queryService,
        FinanceTableKind table,
        string procurementId,
        FinanceField field,
        string? value,
        string? userId,
        CancellationToken ct
    )
    {
        if (!FinanceTable.Fields.TryGetValue(field, out var spec) || spec.Table != table)
            return new BadRequestObjectResult(new { ok = false, error = "Kolom ini tidak bisa diisi dari tabel ini." });

        var change = await trackingService.UpdateFinanceFieldAsync(
            procurementId,
            field,
            value,
            userId ?? string.Empty,
            ct
        );
        if (!change.Success)
            return new OkObjectResult(new { ok = false, error = change.Error });

        var procurement = await queryService.GetProcurementByIdAsync(procurementId);
        var row = procurement == null ? null : FinanceTable.ToRow(procurement);
        var done = table switch
        {
            FinanceTableKind.ApPo => row?.ApPoDone,
            FinanceTableKind.ApInvoice => row?.ApInvoiceDone,
            _ => row?.ArDone,
        };

        return new OkObjectResult(new { ok = true, display = change.Display, done = done == true });
    }
}
