using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDashboardRepository
    {
        Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int take = 10);
        // ApprovalStatusCountDto removed - approval per-document sudah dihapus

        // Dashboard Metrics
        Task<int> GetActiveProcurementsCountAsync(CancellationToken ct = default);
        Task<int> GetPendingApprovalsCountAsync(CancellationToken ct = default);
        Task<int> GetTotalVendorsCountAsync(CancellationToken ct = default);
        Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default);
        Task<decimal> GetTotalCostAsync(CancellationToken ct = default);
        Task<decimal> GetTotalProfitAsync(CancellationToken ct = default);

        // Lists and Details
        Task<List<ProcurementSummary>> GetRecentProcurementsAsync(
            int take = 10,
            CancellationToken ct = default
        );
        Task<List<ApprovalSummary>> GetPendingApprovalsDetailAsync(
            int take = 10,
            CancellationToken ct = default
        );
        Task<List<JobTypeCount>> GetJobTypeDistributionAsync(CancellationToken ct = default);
        Task<List<VendorPerformance>> GetTopVendorsAsync(
            int take = 10,
            CancellationToken ct = default
        );
        Task<List<PurchaseRequisitionSummary>> GetRecentPurchaseRequisitionsAsync(
            int take = 10,
            CancellationToken ct = default
        );
        Task<int> GetTotalPurchaseRequisitionsCountAsync(CancellationToken ct = default);

        // Trends and Charts
        Task<List<MonthlyTrend>> GetMonthlyProcurementTrendAsync(CancellationToken ct = default);
        Task<List<MonthlyTrend>> GetMonthlyPurchaseRequisitionTrendAsync(
            CancellationToken ct = default
        );
        Task<List<StatusCount>> GetProcurementsByStatusAsync(CancellationToken ct = default);
        Task<List<StatusCount>> GetDocumentApprovalStatsAsync(CancellationToken ct = default);

        // User Activity
        Task<List<RecentLoginSummary>> GetUserActivityStatusAsync(
            int take = 30,
            CancellationToken ct = default
        );
    }
}
