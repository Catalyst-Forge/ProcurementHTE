using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models;

public partial class Procurement
{


    /// <summary>
    /// Selected symptoms during rejection (Flags enum)
    /// </summary>
    [DisplayName("Rejection Symptoms")]
    public RejectionSymptom? RejectionSymptoms { get; set; }

    /// <summary>
    /// Symptoms that still need to be addressed (for sequential revision)
    /// </summary>
    [DisplayName("Pending Revision Symptoms")]
    public RejectionSymptom? PendingRevisionSymptoms { get; set; }

    /// <summary>
    /// Status before rejection (to restore after revision if needed)
    /// </summary>
    [DisplayName("Status Before Rejection")]
    public ProcurementStatus? StatusBeforeRejection { get; set; }

    /// <summary>
    /// Number of times this procurement has been revised
    /// </summary>
    [DisplayName("Revision Count")]
    public int RevisionCount { get; set; } = 0;

    /// <summary>
    /// When the procurement was last resubmitted after revision
    /// </summary>
    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Resubmitted At")]
    public DateTime? ResubmittedAt { get; set; }

    /// <summary>
    /// User who resubmitted after revision
    /// </summary>
    [MaxLength(450)]
    [DisplayName("Resubmitted By")]
    public string? ResubmittedByUserId { get; set; }

    // LDP Document Fields (Rekapan Dokumen Procurement yang sudah ditanda tangan basah)
    [MaxLength(255)]
    [DisplayName("LDP File Name")]
    public string? LdpFileName { get; set; }

    [MaxLength(500)]
    [DisplayName("LDP File Path")]
    public string? LdpFileObjectKey { get; set; }

    [MaxLength(100)]
    [DisplayName("LDP File Content Type")]
    public string? LdpFileContentType { get; set; }

    [DisplayName("LDP File Size")]
    public long? LdpFileSize { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("LDP Uploaded At")]
    public DateTime? LdpUploadedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("LDP Uploaded By")]
    public string? LdpUploadedByUserId { get; set; }

    // Approval QR Code Token Fields
    [MaxLength(100)]
    [DisplayName("Approval Token")]
    public string? ApprovalToken { get; set; }

    [DisplayFormat(DataFormatString = "{0:d MMM yyyy HH:mm}", ApplyFormatInEditMode = false)]
    [DisplayName("Approval Token Generated At")]
    public DateTime? ApprovalTokenGeneratedAt { get; set; }

    [MaxLength(450)]
    [DisplayName("Approval Sent By")]
    public string? ApprovalSentByUserId { get; set; }

    // ===== End Procurement-Level Tracking Fields =====

}
