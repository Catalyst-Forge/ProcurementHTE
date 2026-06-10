using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels;

public sealed class ProcurementDetailsPageViewModel
{
    public Procurement Procurement { get; init; } = default!;
    public ProfitLossSummaryViewModel? ProfitLoss { get; init; }
    public IReadOnlyList<ProcDocuments> Documents { get; init; } = [];
    public string? PicOpsName { get; init; }
    public string? AnalystName { get; init; }
    public string? AssistantManagerName { get; init; }
    public string? ManagerName { get; init; }
    public bool CanEditDelete { get; init; }
    public bool CanPublish { get; init; }
    public bool CanUnpublish { get; init; }

    public IReadOnlyList<string> SelectedVendorNames =>
        ProfitLoss?.SelectedVendorNames ?? [];

    public string JobTypeName => Procurement.JobType?.TypeName ?? "Angkutan";
    public bool IsPengangkutan => JobTypeName == "Angkutan";
    public bool IsSewaUnit => JobTypeName == "StandBy";
    public bool IsMoving => JobTypeName == "Moving";
}
