using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class ApInvoiceDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;

        // Statistics
        public int TotalProcurements { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
        public int ProcurementsWithAccrual { get; set; }

        // Recent data
        public List<ProcurementSummary> RecentProcurementsSummary { get; set; } = new();
    }
}
