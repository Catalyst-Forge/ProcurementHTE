using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class WorkOrderCreateViewModel
    {
        public WorkOrder WorkOrder { get; set; } = new();

        // Form akan mem-bind ke properti ini
        public List<WoDetail> Details { get; set; } =
            new()
            {
                new WoDetail(), // baris awal kosong
            };
    }
}
