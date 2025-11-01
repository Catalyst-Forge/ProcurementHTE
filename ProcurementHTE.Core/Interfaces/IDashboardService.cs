using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDashboardService
    {
        Task<IReadOnlyList<WoStatusCountDto>> GetWoStatusCountsAsync();
        Task<IReadOnlyList<RevenuePerMonthDto>> GetRevenuePerMonthAsync(int year);
        Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(int take = 10);
        Task<IReadOnlyList<ApprovalStatusCountDto>> GetApprovalStatusCountsAsync();
    }
}
