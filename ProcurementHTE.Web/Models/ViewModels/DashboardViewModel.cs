using System.ComponentModel;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels
{
    /// <summary>
    /// Main Dashboard ViewModel - aggregates all dashboard data
    /// </summary>
    public class DashboardViewModel
    {
        // Core Metrics
        public int TotalProcurements { get; set; }
        public int ActiveProcurements { get; set; }
        public int PendingApprovals { get; set; }
        public int TotalVendors { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }

        // Financial Metrics
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
        public decimal ProfitMargin =>
            TotalRevenue > 0 ? Math.Round((TotalProfit / TotalRevenue) * 100, 2) : 0;

        // Role-based Data
        public string RoleName { get; set; } = string.Empty;

        // Procurement Lists - using ViewModels
        public List<ProcurementSummaryViewModel> RecentProcurements { get; set; } = new();
        public IReadOnlyList<DashboardProcurementViewModel> RecentProcurementsList { get; set; } =
            Array.Empty<DashboardProcurementViewModel>();

        // Status and Trends - using ViewModels
        public List<StatusCountViewModel> ProcurementsByStatus { get; set; } = new();
        public IReadOnlyList<ProcurementStatusCountDto> ProcurementStatusCounts { get; set; } =
            Array.Empty<ProcurementStatusCountDto>();

        public List<MonthlyTrendViewModel> MonthlyProcurementTrend { get; set; } = new();
        public IReadOnlyList<RevenuePerMonthDto> RevenuePerMonth { get; set; } =
            Array.Empty<RevenuePerMonthDto>();

        // Distribution and Analytics - using ViewModels
        public List<JobTypeCountViewModel> JobTypeDistribution { get; set; } = new();
        public List<StatusCountViewModel> DocumentApprovalStats { get; set; } = new();
        // ApprovalStatus removed - approval per-document sudah dihapus

        // Approvals - using ViewModels
        public List<ApprovalSummaryViewModel> PendingApprovalsDetail { get; set; } = new();

        // Vendors - using ViewModels
        public List<VendorPerformanceViewModel> TopVendors { get; set; } = new();

        // Activities - using ViewModels
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();

        // Purchase Requisition - using ViewModels
        public int TotalPurchaseRequisitions { get; set; }
        public List<PurchaseRequisitionSummaryViewModel> RecentPurchaseRequisitions { get; set; } =
            new();
        public List<MonthlyTrendViewModel> MonthlyPurchaseRequisitionTrend { get; set; } = new();

        // User Activity - using ViewModels
        public int OnlineUsersCount { get; set; }
        public List<UserActivityViewModel> UserActivities { get; set; } = new();

        // Backward compatibility - deprecated, use UserActivities
        [Obsolete("Use UserActivities instead")]
        public List<UserActivityViewModel> RecentLogins
        {
            get => UserActivities;
            set => UserActivities = value;
        }

        // Admin-only: Extended Metrics
        public int PendingAccrualCount { get; set; }
        public int FilledAccrualCount { get; set; }
        public decimal TotalPotensiAccrual { get; set; }
        public int AccrualFillPercentage => TotalProcurements > 0
            ? (int)Math.Round((decimal)FilledAccrualCount / TotalProcurements * 100)
            : 0;

        // Role Distribution
        public List<RoleDistributionViewModel> RoleDistribution { get; set; } = new();

        // Region Distribution
        public List<RegionDistributionViewModel> RegionDistribution { get; set; } = new();
    }












}

