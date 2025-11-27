using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class DashboardSummaryViewModel
    {
        public string RoleName { get; set; } = string.Empty;
        public IReadOnlyList<DashboardProcurementViewModel> RecentProcurements { get; set; } =
            Array.Empty<DashboardProcurementViewModel>();

        public IReadOnlyList<ProcurementStatusCountDto> ProcurementsByStatus { get; set; } =
            Array.Empty<ProcurementStatusCountDto>();

        public IReadOnlyList<RevenuePerMonthDto> RevenuePerMonth { get; set; } =
            Array.Empty<RevenuePerMonthDto>();

        public IReadOnlyList<ApprovalStatusCountDto> ApprovalStatus { get; set; } =
            Array.Empty<ApprovalStatusCountDto>();

        public IReadOnlyList<RecentActivityDto> RecentActivities { get; set; } =
            Array.Empty<RecentActivityDto>();

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalProcurements { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
    }
}
