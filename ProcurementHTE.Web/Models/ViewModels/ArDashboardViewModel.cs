using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class ArDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;

        // Statistics
        public int TotalProcurements { get; set; }
        public int PendingAccrual { get; set; }
        public int FilledAccrual { get; set; }
        public decimal TotalPotensiAccrual { get; set; }

        // Percentage
        public double FillPercentage => TotalProcurements > 0
            ? Math.Round((double)FilledAccrual / TotalProcurements * 100, 1)
            : 0;

        // Recent data
        public List<Procurement> RecentFilledByMe { get; set; } = new();
        public List<Procurement> RecentPending { get; set; } = new();
    }
}
