using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

public static partial class ProcurementSeeder
{
    private sealed record ProcurementSeedUsers(
        User CreatedBy,
        User PicOpsUser,
        User AnalystUser,
        User AssistantManagerUser,
        User ManagerUser
    );

    private sealed record ProcurementSeedJobTypes(
        JobTypes Angkutan,
        JobTypes StandBy,
        JobTypes Moving
    );

    private sealed record ProcurementSeedData
    {
        public required string JobTypeName { get; init; }
        public required string StatusName { get; init; }
        public required ContractType ContractType { get; init; }
        public required ProcurementCategory ProcurementCategory { get; init; }
        public required string JobName { get; init; }
        public required string SpkNumber { get; init; }
        public required string Wonum { get; init; }
        public required DateTime StartDate { get; init; }
        public required DateTime EndDate { get; init; }
        public required ProjectRegion ProjectRegion { get; init; }
        public required DateTime PotentialAccrualDate { get; init; }
        public required string SpmpNumber { get; init; }
        public required string MemoNumber { get; init; }
        public required string OeNumber { get; init; }
        public required string RaNumber { get; init; }
        public required string ProjectCode { get; init; }
        public required string LtcName { get; init; }
        public required string Note { get; init; }
        public required DateTime CreatedAt { get; init; }
        public List<ProcDetailSeed> Details { get; init; } = new();
    }

    private sealed record ProcDetailSeed(string ItemName, int Quantity, string Unit);
}
