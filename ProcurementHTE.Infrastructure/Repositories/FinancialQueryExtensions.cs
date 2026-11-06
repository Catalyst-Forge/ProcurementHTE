using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Repositories;

/// <summary>
/// Limits P&amp;L figures to what the dashboards report. Deleted records are gone, and a cancelled
/// job keeps its P&amp;L for the record but no longer earns or spends anything.
/// </summary>
internal static class FinancialQueryExtensions
{
    public static IQueryable<ProfitLoss> CountedInFinancials(this IQueryable<ProfitLoss> profitLosses) =>
        profitLosses.Where(pnl =>
            !pnl.IsDeleted
            && !pnl.Procurement.IsDeleted
            && pnl.Procurement.ProcurementStatus != ProcurementStatus.Cancelled
        );

    public static IQueryable<ProfitLossItem> CountedInFinancials(this IQueryable<ProfitLossItem> items) =>
        items.Where(item =>
            !item.ProfitLoss.IsDeleted
            && !item.ProfitLoss.Procurement.IsDeleted
            && item.ProfitLoss.Procurement.ProcurementStatus != ProcurementStatus.Cancelled
        );
}
