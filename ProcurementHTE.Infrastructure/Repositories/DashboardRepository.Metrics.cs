using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class DashboardRepository
    {
        public async Task<int> GetActiveProcurementsCountAsync(CancellationToken ct = default)
        {
            return await _context
                .Procurements.Where(p => p.Status != null)
                .CountAsync(
                    p => p.Status!.StatusName == "Created" || p.Status!.StatusName == "In Progress",
                    ct
                );
        }

        public async Task<int> GetPendingApprovalsCountAsync(CancellationToken ct = default)
        {
            return await _context.PurchaseRequisitions.CountAsync(
                pr =>
                    pr.Status == PurchaseRequisitionStatus.WaitingApprovalAnalyst
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalAsstManager
                    || pr.Status == PurchaseRequisitionStatus.WaitingApprovalManager,
                ct
            );
        }

        public async Task<int> GetTotalVendorsCountAsync(CancellationToken ct = default)
        {
            return await _context.Vendors.CountAsync(ct);
        }

        public async Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default)
        {
            return await _context.ProfitLossItems.SumAsync(pnl => (decimal?)pnl.Revenue, ct) ?? 0m;
        }

        public async Task<decimal> GetTotalCostAsync(CancellationToken ct = default)
        {
            return await _context.ProfitLosses.SumAsync(
                    pnl => (decimal?)pnl.SelectedVendorFinalOffer,
                    ct
                ) ?? 0m;
        }

        public async Task<decimal> GetTotalProfitAsync(CancellationToken ct = default)
        {
            return await _context.ProfitLosses.SumAsync(pnl => (decimal?)pnl.Profit, ct) ?? 0m;
        }

        public async Task<int> GetTotalPurchaseRequisitionsCountAsync(
            CancellationToken ct = default
        )
        {
            return await _context.PurchaseRequisitions.CountAsync(ct);
        }
    }
}
