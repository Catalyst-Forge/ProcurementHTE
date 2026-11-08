using System.ComponentModel.DataAnnotations;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.Admin;

public class DeletedRecordsViewModel
{
    public DeletedRecordKind Kind { get; init; }
    public IReadOnlyDictionary<DeletedRecordKind, int> Counts { get; init; } = new Dictionary<DeletedRecordKind, int>();
    public IReadOnlyList<DeletedRecordItem> Items { get; init; } = [];

    public static readonly IReadOnlyList<(DeletedRecordKind Kind, string Label)> Tabs =
    [
        (DeletedRecordKind.Procurement, "Procurements"),
        (DeletedRecordKind.PurchaseRequisition, "PR (Purchase Requisition)"),
        (DeletedRecordKind.Vendor, "Vendors"),
        (DeletedRecordKind.JobType, "Procurement Types"),
        (DeletedRecordKind.DocumentType, "Document Types"),
        (DeletedRecordKind.JobTypeDocument, "Job Type Documents"),
        (DeletedRecordKind.DocumentApproval, "Document Approvals"),
        (DeletedRecordKind.DocumentApprovalRule, "Approval Rules"),
        (DeletedRecordKind.User, "Users"),
        (DeletedRecordKind.LdpArchive, "Arsip LDP"),
    ];

    public static string LabelFor(string kind) =>
        Enum.TryParse<DeletedRecordKind>(kind, out var parsed)
            ? Tabs.FirstOrDefault(tab => tab.Kind == parsed).Label ?? kind
            : kind;
}

public class PurgePageViewModel
{
    public required PurgePreview Preview { get; init; }
    public required PurgeInputModel Form { get; init; }
}

public class PurgeInputModel
{
    public const string ConfirmationText = "HAPUS";

    public DeletedRecordKind Kind { get; set; }

    [Required]
    public string Id { get; set; } = default!;

    [Required(ErrorMessage = "Alasan wajib diisi.")]
    [StringLength(500, MinimumLength = PurgeRequest.MinReasonLength, ErrorMessage = "Alasan 10 sampai 500 karakter.")]
    [Display(Name = "Alasan")]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ketik HAPUS untuk mengonfirmasi.")]
    [Display(Name = "Konfirmasi")]
    public string Confirmation { get; set; } = string.Empty;
}
