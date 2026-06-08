using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models;

public partial class Procurement
{
    // Accrual Fields (diisi oleh AR)
    [MaxLength(100)]
    [DisplayName("No. Accrual")]
    public string? NoAccrual { get; set; }

    [DisplayName("Potensi Accrual")]
    public decimal? PotensiAccrual { get; set; }

    [MaxLength(100)]
    [DisplayName("Status Accrual")]
    public string? StatusAccrual { get; set; }

    [MaxLength(450)]
    [DisplayName("Accrual Filled By")]
    public string? AccrualFilledByUserId { get; set; }

    [DisplayName("Accrual Filled At")]
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    public DateTime? AccrualFilledAt { get; set; }

    // ===== Invoice Fields (filled by AP-Invoice role) =====

    [MaxLength(100)]
    [DisplayName("SA No")]
    public string? SANo { get; set; }

    [MaxLength(100)]
    [DisplayName("SP 3 No")]
    public string? SP3No { get; set; }

    [MaxLength(450)]
    [DisplayName("AP-Invoice User")]
    public string? ApInvoiceUserId { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("AP-Invoice Picked Up At")]
    public DateTime? ApInvoicePickedUpAt { get; set; }

    // ===== AR Pickup Fields =====

    [MaxLength(450)]
    [DisplayName("AR User")]
    public string? ArUserId { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("AR Picked Up At")]
    public DateTime? ArPickedUpAt { get; set; }

    // ===== Procurement-Level Tracking Fields (NEW) =====

    /// <summary>
    /// Status tracking untuk procurement individual (menggantikan StatusId)
    /// </summary>
    [DisplayName("Procurement Status")]
    public ProcurementStatus ProcurementStatus { get; set; } = ProcurementStatus.OnCreateDP3;

    // ISPA Tracking Fields
    [MaxLength(100)]
    [DisplayName("ISPA Number")]
    public string? IspaNumber { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Tanggal ISPA")]
    public DateTime? IspaDate { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy}", ApplyFormatInEditMode = false)]
    [DisplayName("Tanggal Submit ISPA")]
    public DateTime? IspaSubmitDate { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("ISPA Submitted At")]
    public DateTime? IspaSubmittedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("ISPA Submitted By")]
    public string? IspaSubmittedByUserId { get; set; }

    // ISPA File Fields
    [MaxLength(255)]
    [DisplayName("ISPA File Name")]
    public string? IspaFileName { get; set; }

    [MaxLength(500)]
    [DisplayName("ISPA File Path")]
    public string? IspaFileObjectKey { get; set; }

    [MaxLength(100)]
    [DisplayName("ISPA File Content Type")]
    public string? IspaFileContentType { get; set; }

    [DisplayName("ISPA File Size")]
    public long? IspaFileSize { get; set; }

    // PO Tracking Fields
    [MaxLength(100)]
    [DisplayName("PO Number")]
    public string? PoNumber { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("PO Submitted At")]
    public DateTime? PoSubmittedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("PO Submitted By")]
    public string? PoSubmittedByUserId { get; set; }

    // Hardcopy Evidence Tracking Fields
    [MaxLength(255)]
    [DisplayName("Hardcopy Evidence File Name")]
    public string? HardcopyEvidenceFileName { get; set; }

    [MaxLength(500)]
    [DisplayName("Hardcopy Evidence File Path")]
    public string? HardcopyEvidenceFilePath { get; set; }

    [MaxLength(100)]
    [DisplayName("Hardcopy Evidence Content Type")]
    public string? HardcopyEvidenceContentType { get; set; }

    [DisplayName("Hardcopy Evidence File Size")]
    public long? HardcopyEvidenceFileSize { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Hardcopy Submitted At")]
    public DateTime? HardcopySubmittedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("Hardcopy Submitted By")]
    public string? HardcopySubmittedByUserId { get; set; }

    // Rejection Tracking Fields
    [MaxLength(1000)]
    [DisplayName("Rejection Note")]
    public string? RejectionNote { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Rejected At")]
    public DateTime? RejectedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("Rejected By")]
    public string? RejectedByUserId { get; set; }

    // ===== Revision Tracking Fields =====


}
