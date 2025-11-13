using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProcurementHTE.Core.Models
{
    public enum ProcurementType
    {
        Barang = 1,
        Jasa = 2,
    }

    public class WorkOrder
    {
        [Key]
        public string WorkOrderId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string WoNum { get; set; } = null!;

        [DisplayName("Deskripsi")]
        public string? Description { get; set; }

        [DisplayName("Catatan")]
        [MaxLength(1000)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Jenis pengadaan harus dipilih")]
        [DisplayName("Jenis Pengadaan")]
        public ProcurementType ProcurementType { get; set; }

        [Required(ErrorMessage = "Kolom Work Order No. harus diisi")]
        [DisplayName("W.O. No.")]
        public string WoNumLetter { get; set; } = null!;

        [Required(ErrorMessage = "Tanggal Surat harus dipilih")]
        [DisplayName("Tanggal Surat")]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DateLetter { get; set; }

        [Required(ErrorMessage = "Kolom Dari harus diisi")]
        [DisplayName("Dari")]
        public string From { get; set; } = null!;

        [Required(ErrorMessage = "Kolom Kepada harus diisi")]
        [DisplayName("Kepada")]
        public string To { get; set; } = null!;

        [DisplayName("Perintah Kerja")]
        public string? WorkOrderLetter { get; set; }

        public string? WBS { get; set; }

        [Required(ErrorMessage = "Kolom GL Account harus diisi")]
        [DisplayName("GL Account")]
        public string GlAccount { get; set; } = null!;

        [Required(ErrorMessage = "Tanggal Diperlukan harus dipilih")]
        [DisplayName("Tanggal Diperlukan")]
        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime DateRequired { get; set; }

        [Required(ErrorMessage = "Kolom Bagian Peminta harus diisi")]
        [DisplayName("Bagian Peminta")]
        public string Requester { get; set; } = null!;

        [Required(ErrorMessage = "Kolom Disetujui oleh harus diisi")]
        [DisplayName("Disetujui oleh")]
        public string Approved { get; set; } = null!;

        public string? XS1 { get; set; }

        public string? XS2 { get; set; }

        public string? XS3 { get; set; }

        public string? XS4 { get; set; }

        public string? FileWorkOrder { get; set; }

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? UpdatedAt { get; set; }

        [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
        public DateTime? CompletedAt { get; set; }

        // Foreign Keys
        public string? WoTypeId { get; set; }
        public int StatusId { get; set; }
        public string? UserId { get; set; }

        [ForeignKey("WoTypeId")]
        [JsonIgnore]
        public WoTypes? WoType { get; set; }

        [ForeignKey("StatusId")]
        [JsonIgnore]
        public Status? Status { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }

        public ICollection<WoOffer> WoOffers { get; set; } = [];
        public ICollection<WoDocuments>? WoDocuments { get; set; } = [];
        public ICollection<WoDetail>? WoDetails { get; set; } = [];
        public ICollection<VendorOffer> VendorOffers { get; set; } = [];
    }
}
