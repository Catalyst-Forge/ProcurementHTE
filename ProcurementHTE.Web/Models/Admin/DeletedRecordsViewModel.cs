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
        (DeletedRecordKind.PurchaseRequisition, "PR Service"),
        (DeletedRecordKind.Vendor, "Vendors"),
        (DeletedRecordKind.JobType, "Procurement Types"),
        (DeletedRecordKind.DocumentType, "Document Types"),
        (DeletedRecordKind.JobTypeDocument, "Job Type Documents"),
        (DeletedRecordKind.DocumentApproval, "Document Approvals"),
        (DeletedRecordKind.DocumentApprovalRule, "Approval Rules"),
        (DeletedRecordKind.User, "Users"),
    ];
}
