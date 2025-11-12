using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class WorkOrderCreateViewModel
    {
        public WorkOrder WorkOrder { get; set; } = new();

        public List<WoDetail> Details { get; set; } = [];
        public List<WoOffer> Offers { get; set; } = [];
    }

    public class WorkOrderEditViewModel
    {
        [Required]
        public string WorkOrderId { get; set; } = default!;

        [DisplayName("WO Number")]
        public string? WoNum { get; set; }

        [DisplayName("Description")]
        public string? Description { get; set; }

        [DisplayName("Note")]
        [MaxLength(1000)]
        public string? Note { get; set; }


        [Required(ErrorMessage = "Tipe Work Order harus dipilih")]
        [DisplayName("Work Order Type")]
        public string? WoTypeId { get; set; }

        public ProcurementType ProcurementType { get; set; }


        [DisplayName("WO Letter Number")]
        public string? WoNumLetter { get; set; }

        [DisplayName("Date Letter")]
        [DataType(DataType.Date)]
        public DateTime? DateLetter { get; set; }

        [DisplayName("From")]
        public string? From { get; set; }

        [DisplayName("To")]
        public string? To { get; set; }

        [DisplayName("Work Order Letter")]
        public string? WorkOrderLetter { get; set; }

        [DisplayName("WBS")]
        public string? WBS { get; set; }

        [DisplayName("GL Account")]
        public string? GlAccount { get; set; }

        [DisplayName("Date Required")]
        [DataType(DataType.Date)]
        public DateTime? DateRequired { get; set; }


        [DisplayName("Requester")]
        public string? Requester { get; set; }

        [DisplayName("Approved By")]
        public string? Approved { get; set; }


        public string? XS1 { get; set; }
        public string? XS2 { get; set; }
        public string? XS3 { get; set; }
        public string? XS4 { get; set; }

        public int StatusId { get; set; }

        // Collections
        public List<WoDetail> Details { get; set; } = [];
        public List<WoOffer> Offers { get; set; } = [];

        // Lookup Data
        public IEnumerable<WoTypes>? WoTypes { get; set; }
        public IEnumerable<Status>? Statuses { get; set; }
    }
}
