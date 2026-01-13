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

    #region Dashboard Detail ViewModels

    public class DashboardProcurementViewModel
    {
        [DisplayName("Procurement No.")]
        public string ProcNum { get; set; } = string.Empty;

        [DisplayName("Job Name")]
        public string? JobName { get; set; }

        [DisplayName("Job Type")]
        public string? JobTypeName { get; set; }

        [DisplayName("Status")]
        public string StatusName { get; set; } = string.Empty;

        [DisplayName("Created At")]
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Procurement ViewModels

    public class ProcurementSummaryViewModel
    {
        [DisplayName("Proc No.")]
        public string ProcNum { get; set; } = string.Empty;

        [DisplayName("Job Type")]
        public string JobTypeName { get; set; } = string.Empty;

        [DisplayName("Status")]
        public string StatusName { get; set; } = string.Empty;

        [DisplayName("Created By")]
        public string CreatedBy { get; set; } = string.Empty;

        [DisplayName("Created Date")]
        public DateTime CreatedDate { get; set; }

        [DisplayName("Total Amount")]
        public decimal TotalAmount { get; set; }
    }

    #endregion

    #region Approval ViewModels

    public class ApprovalSummaryViewModel
    {
        [DisplayName("Proc No.")]
        public string ProcNum { get; set; } = string.Empty;

        [DisplayName("Document")]
        public string DocumentName { get; set; } = string.Empty;

        [DisplayName("Approval Role")]
        public string ApprovalRole { get; set; } = string.Empty;

        [DisplayName("Created Date")]
        public DateTime CreatedDate { get; set; }

        [DisplayName("Days Waiting")]
        public int DaysWaiting => (DateTime.Now - CreatedDate).Days;
    }

    #endregion

    #region Job Type ViewModels

    public class JobTypeCountViewModel
    {
        [DisplayName("Job Type")]
        public string JobTypeName { get; set; } = string.Empty;

        [DisplayName("Count")]
        public int Count { get; set; }

        [DisplayName("Total Value")]
        public decimal TotalValue { get; set; }
    }

    #endregion

    #region Vendor ViewModels

    public class VendorPerformanceViewModel
    {
        [DisplayName("Vendor Code")]
        public string VendorCode { get; set; } = string.Empty;

        [DisplayName("Vendor Name")]
        public string VendorName { get; set; } = string.Empty;

        [DisplayName("Offer Count")]
        public int OfferCount { get; set; }

        [DisplayName("Selected Count")]
        public int SelectedCount { get; set; }

        [DisplayName("Win Rate")]
        public decimal WinRate =>
            OfferCount > 0 ? Math.Round((decimal)SelectedCount / OfferCount * 100, 2) : 0;
    }

    #endregion

    #region Purchase Requisition ViewModels

    public class PurchaseRequisitionSummaryViewModel
    {
        [DisplayName("PR ID")]
        public string PrId { get; set; } = string.Empty;

        [DisplayName("PR Number")]
        public string PrNumber { get; set; } = string.Empty;

        [DisplayName("Request Date")]
        public DateTime RequestDate { get; set; }

        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("Created By")]
        public string CreatedBy { get; set; } = string.Empty;

        [DisplayName("Created At")]
        public DateTime CreatedAt { get; set; }

        [DisplayName("Procurement Count")]
        public int ProcurementCount { get; set; }
    }

    #endregion

    #region Trend ViewModels

    public class MonthlyTrendViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public string MonthYear => $"{GetMonthName(Month)} {Year}";

        private string GetMonthName(int month)
        {
            return new DateTime(2000, month, 1).ToString("MMM");
        }
    }

    #endregion

    #region Status ViewModels

    public class StatusCountViewModel
    {
        [DisplayName("Status")]
        public string StatusName { get; set; } = string.Empty;

        [DisplayName("Count")]
        public int Count { get; set; }
    }

    #endregion

    #region User Activity ViewModels

    public class UserActivityViewModel
    {
        public string UserId { get; set; } = string.Empty;

        [DisplayName("Full Name")]
        public string FullName { get; set; } = string.Empty;

        [DisplayName("Username")]
        public string UserName { get; set; } = string.Empty;

        [DisplayName("Job Title")]
        public string? JobTitle { get; set; }

        [DisplayName("Last Login")]
        public DateTime LastLoginAt { get; set; }

        [DisplayName("Online Status")]
        public bool IsOnline { get; set; }

        [DisplayName("Status")]
        public string StatusTime => GetStatusTime();

        private string GetStatusTime()
        {
            var timeSpan = DateTime.Now - LastLoginAt;
            if (IsOnline)
            {
                if (timeSpan.TotalMinutes < 1)
                    return "Online - Baru saja";
                if (timeSpan.TotalMinutes < 60)
                    return $"Online - {(int)timeSpan.TotalMinutes} menit yang lalu";
                return $"Online - {(int)timeSpan.TotalHours} jam yang lalu";
            }
            else
            {
                if (timeSpan.TotalMinutes < 60)
                    return $"Offline - {(int)timeSpan.TotalMinutes} menit yang lalu";
                if (timeSpan.TotalHours < 24)
                    return $"Offline - {(int)timeSpan.TotalHours} jam yang lalu";
                if (timeSpan.TotalDays < 7)
                    return $"Offline - {(int)timeSpan.TotalDays} hari yang lalu";
                return $"Offline - {LastLoginAt:dd MMM yyyy}";
            }
        }
    }

    #endregion

    #region Recent Activity ViewModels

    public class RecentActivityViewModel
    {
        [DisplayName("Time")]
        public DateTime Time { get; set; }

        [DisplayName("User")]
        public string User { get; set; } = string.Empty;

        [DisplayName("Action")]
        public string Action { get; set; } = string.Empty;

        [DisplayName("Description")]
        public string? Description { get; set; }
    }

    #endregion

    #region Role Distribution ViewModels

    public class RoleDistributionViewModel
    {
        [DisplayName("Role Name")]
        public string RoleName { get; set; } = string.Empty;

        [DisplayName("User Count")]
        public int UserCount { get; set; }

        [DisplayName("Color")]
        public string Color { get; set; } = "#6c757d";
    }

    #endregion

    #region Region Distribution ViewModels

    public class RegionDistributionViewModel
    {
        [DisplayName("Region")]
        public string RegionName { get; set; } = string.Empty;

        [DisplayName("Count")]
        public int Count { get; set; }

        [DisplayName("Total Value")]
        public decimal TotalValue { get; set; }
    }

    #endregion
}
