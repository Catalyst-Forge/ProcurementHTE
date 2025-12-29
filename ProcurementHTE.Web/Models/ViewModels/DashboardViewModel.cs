using System.ComponentModel;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels
{
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

        // Procurement Lists
        public List<ProcurementSummary> RecentProcurements { get; set; } = new();
        public IReadOnlyList<DashboardProcurementViewModel> RecentProcurementsList { get; set; } =
            Array.Empty<DashboardProcurementViewModel>();

        // Status and Trends
        public List<StatusCount> ProcurementsByStatus { get; set; } = new();
        public IReadOnlyList<ProcurementStatusCountDto> ProcurementStatusCounts { get; set; } =
            Array.Empty<ProcurementStatusCountDto>();

        public List<MonthlyTrend> MonthlyProcurementTrend { get; set; } = new();
        public IReadOnlyList<RevenuePerMonthDto> RevenuePerMonth { get; set; } =
            Array.Empty<RevenuePerMonthDto>();

        // Distribution and Analytics
        public List<JobTypeCount> JobTypeDistribution { get; set; } = new();
        public List<StatusCount> DocumentApprovalStats { get; set; } = new();
        public IReadOnlyList<ApprovalStatusCountDto> ApprovalStatus { get; set; } =
            Array.Empty<ApprovalStatusCountDto>();

        // Approvals
        public List<ApprovalSummary> PendingApprovalsDetail { get; set; } = new();

        // Vendors
        public List<VendorPerformance> TopVendors { get; set; } = new();

        // Activities
        public IReadOnlyList<RecentActivityDto> RecentActivities { get; set; } =
            Array.Empty<RecentActivityDto>();

        // Purchase Requisition
        public int TotalPurchaseRequisitions { get; set; }
        public List<PurchaseRequisitionSummary> RecentPurchaseRequisitions { get; set; } = new();
    }

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

    public class StatusCount
    {
        public string StatusName { get; set; }
        public int Count { get; set; }
    }

    public class MonthlyTrend
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

    public class ProcurementSummary
    {
        public string ProcNum { get; set; }
        public string JobTypeName { get; set; }
        public string StatusName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ApprovalSummary
    {
        public string ProcNum { get; set; }
        public string DocumentName { get; set; }
        public string ApprovalRole { get; set; }
        public DateTime CreatedDate { get; set; }
        public int DaysWaiting => (DateTime.Now - CreatedDate).Days;
    }

    public class JobTypeCount
    {
        public string JobTypeName { get; set; }
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class VendorPerformance
    {
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
        public int OfferCount { get; set; }
        public int SelectedCount { get; set; }
        public decimal WinRate =>
            OfferCount > 0 ? Math.Round((decimal)SelectedCount / OfferCount * 100, 2) : 0;
    }

    public class PurchaseRequisitionSummary
    {
        public string PrId { get; set; } = string.Empty;
        public string PrNumber { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string? Description { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ProcurementCount { get; set; }
    }
}
