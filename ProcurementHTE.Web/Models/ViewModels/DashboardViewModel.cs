using PdfSharpCore.Pdf.Content.Objects;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProcurements { get; set; }
        public int ActiveProcurements { get; set; }
        public int PendingApprovals { get; set; }
        public int TotalVendors { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMargin =>
            TotalRevenue > 0 ? Math.Round((TotalProfit / TotalRevenue) * 100, 2) : 0;

        public List<StatusCount> ProcurementsByStatus { get; set; } = new();
        public List<MonthlyTrend> MonthlyProcurementTrend { get; set; } = new();
        public List<JobTypeCount> JobTypeDistribution { get; set; } = new();
        public List<StatusCount> DocumentApprovalStats { get; set; } = new();

        public List<ProcurementSummary> RecentProcurements { get; set; } = new();
        public List<ApprovalSummary> PendingApprovalsDetail { get; set; } = new();
        public List<VendorPerformance> TopVendors { get; set; } = new();

        // Purchase Requisition
        public int TotalPurchaseRequisitions { get; set; }
        public List<PurchaseRequisitionSummary> RecentPurchaseRequisitions { get; set; } = new();
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
