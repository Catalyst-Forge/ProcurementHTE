using System.ComponentModel;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels
{
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
}
