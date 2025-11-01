using System.ComponentModel;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class DashboardWoViewModel
    {
        [DisplayName("WO No.")]
        public string WoNum { get; set; } = string.Empty;

        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("Type")]
        public ProcurementType ProcurementType { get; set; }

        [DisplayName("Status")]
        public string StatusName { get; set; } = string.Empty;

        [DisplayName("Created At")]
        public DateTime CreatedAt { get; set; }
    }
}
