using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IProcurementRepository _procurementRepository;
        private readonly IProfitLossRepository _pnlRepository;
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(
            IProcurementRepository procurementRepository,
            IProfitLossRepository pnlRepository,
            IDashboardRepository dashboardRepository
        )
        {
            _procurementRepository = procurementRepository;
            _pnlRepository = pnlRepository;
            _dashboardRepository = dashboardRepository;
        }

        public async Task<
            IReadOnlyList<ProcurementStatusCountDto>
        > GetProcurementStatusCountsAsync() => await _procurementRepository.GetCountByStatusAsync();

        public async Task<IReadOnlyList<RevenuePerMonthDto>> GetRevenuePerMonthAsync(int year) =>
            await _pnlRepository.GetRevenuePerMonthAsync(year);

        public async Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(
            int take = 10
        ) => await _dashboardRepository.GetRecentActivitiesAsync(take);

        public async Task<IReadOnlyList<ApprovalStatusCountDto>> GetApprovalStatusCountsAsync() =>
            await _dashboardRepository.GetApprovalStatusCountsAsync();

        // Dashboard Metrics - delegating to repository
        public async Task<int> GetActiveProcurementsCountAsync(CancellationToken ct = default) =>
            await _dashboardRepository.GetActiveProcurementsCountAsync(ct);

        public async Task<int> GetPendingApprovalsCountAsync(CancellationToken ct = default) =>
            await _dashboardRepository.GetPendingApprovalsCountAsync(ct);

        public async Task<int> GetTotalVendorsCountAsync(CancellationToken ct = default) =>
            await _dashboardRepository.GetTotalVendorsCountAsync(ct);

        public async Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default) =>
            await _dashboardRepository.GetTotalRevenueAsync(ct);

        public async Task<decimal> GetTotalCostAsync(CancellationToken ct = default) =>
            await _dashboardRepository.GetTotalCostAsync(ct);

        public async Task<decimal> GetTotalProfitAsync(CancellationToken ct = default) =>
            await _dashboardRepository.GetTotalProfitAsync(ct);

        // Lists and Details - delegating to repository
        public async Task<List<ProcurementSummary>> GetRecentProcurementsAsync(
            int take = 10,
            CancellationToken ct = default
        ) => await _dashboardRepository.GetRecentProcurementsAsync(take, ct);

        public async Task<List<ApprovalSummary>> GetPendingApprovalsDetailAsync(
            int take = 10,
            CancellationToken ct = default
        ) => await _dashboardRepository.GetPendingApprovalsDetailAsync(take, ct);

        public async Task<List<JobTypeCount>> GetJobTypeDistributionAsync(
            CancellationToken ct = default
        ) => await _dashboardRepository.GetJobTypeDistributionAsync(ct);

        public async Task<List<VendorPerformance>> GetTopVendorsAsync(
            int take = 10,
            CancellationToken ct = default
        ) => await _dashboardRepository.GetTopVendorsAsync(take, ct);

        public async Task<List<PurchaseRequisitionSummary>> GetRecentPurchaseRequisitionsAsync(
            int take = 10,
            CancellationToken ct = default
        ) => await _dashboardRepository.GetRecentPurchaseRequisitionsAsync(take, ct);

        public async Task<int> GetTotalPurchaseRequisitionsCountAsync(
            CancellationToken ct = default
        ) => await _dashboardRepository.GetTotalPurchaseRequisitionsCountAsync(ct);

        // Trends and Charts - delegating to repository
        public async Task<List<MonthlyTrend>> GetMonthlyProcurementTrendAsync(
            CancellationToken ct = default
        ) => await _dashboardRepository.GetMonthlyProcurementTrendAsync(ct);

        public async Task<List<MonthlyTrend>> GetMonthlyPurchaseRequisitionTrendAsync(
            CancellationToken ct = default
        ) => await _dashboardRepository.GetMonthlyPurchaseRequisitionTrendAsync(ct);

        public async Task<List<StatusCount>> GetProcurementsByStatusAsync(
            CancellationToken ct = default
        ) => await _dashboardRepository.GetProcurementsByStatusAsync(ct);

        public async Task<List<StatusCount>> GetDocumentApprovalStatsAsync(
            CancellationToken ct = default
        ) => await _dashboardRepository.GetDocumentApprovalStatsAsync(ct);

        // User Activity - delegating to repository
        public async Task<List<RecentLoginSummary>> GetUserActivityStatusAsync(
            int take = 30,
            CancellationToken ct = default
        ) => await _dashboardRepository.GetUserActivityStatusAsync(take, ct);
    }
}
