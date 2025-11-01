using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IDashboardRepository
    {
        Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int take = 10);
        Task<IReadOnlyList<ApprovalStatusCountDto>> GetApprovalStatusCountsAsync();
    }
}
