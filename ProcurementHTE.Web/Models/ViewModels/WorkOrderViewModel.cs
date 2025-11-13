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
        public string WoNum { get; set; } = default!;

        [DisplayName("Deskripsi")]
        public string? Description { get; set; }

        [DisplayName("Catatan")]
        [MaxLength(1000)]
        public string? Note { get; set; }

        public string WoTypeId { get; set; } = default!;

        public ProcurementType ProcurementType { get; set; }

        [DisplayName("W.O. No.")]
        public string WoNumLetter { get; set; } = default!;

        [Required]
        [DisplayName("Tanggal Surat")]
        [DataType(DataType.Date)]
        public DateTime DateLetter { get; set; }

        [DisplayName("Perintah Kerja")]
        public string? WorkOrderLetter { get; set; }

        [Required]
        [DisplayName("Tanggal Diperlukan")]
        [DataType(DataType.Date)]
        public DateTime DateRequired { get; set; }

        [DisplayName("Dari")]
        public string From { get; set; } = default!;

        [DisplayName("Kepada")]
        public string To { get; set; } = default!;

        [DisplayName("WBS")]
        public string? WBS { get; set; }

        [DisplayName("GL Account")]
        public string GlAccount { get; set; } = default!;

        [DisplayName("Bagian Peminta")]
        public string Requester { get; set; } = default!;

        [DisplayName("Disetujui oleh")]
        public string Approved { get; set; } = default!;

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
