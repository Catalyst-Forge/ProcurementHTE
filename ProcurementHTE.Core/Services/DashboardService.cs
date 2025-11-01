using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IWorkOrderRepository _woRepository;
        private readonly IProfitLossRepository _pnlRepository;
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(
            IWorkOrderRepository woRepository,
            IProfitLossRepository pnlRepository,
            IDashboardRepository dashboardRepository
        )
        {
            _woRepository = woRepository;
            _pnlRepository = pnlRepository;
            _dashboardRepository = dashboardRepository;
        }

        public async Task<IReadOnlyList<WoStatusCountDto>> GetWoStatusCountsAsync() =>
            await _woRepository.GetCountByStatusAsync();

        public async Task<IReadOnlyList<RevenuePerMonthDto>> GetRevenuePerMonthAsync(int year) =>
            await _pnlRepository.GetRevenuePerMonthAsync(year);

        public async Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(
            int take = 10
        ) => await _dashboardRepository.GetRecentActivitiesAsync(take);

        public async Task<IReadOnlyList<ApprovalStatusCountDto>> GetApprovalStatusCountsAsync() =>
            await _dashboardRepository.GetApprovalStatusCountsAsync();
    }
}
