using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDashboardService
    {
        Task<IReadOnlyList<ProcurementStatusCountDto>> GetProcurementStatusCountsAsync();
        Task<IReadOnlyList<RevenuePerMonthDto>> GetRevenuePerMonthAsync(int year);
        Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(int take = 10);
        Task<IReadOnlyList<ApprovalStatusCountDto>> GetApprovalStatusCountsAsync();
    }
}
