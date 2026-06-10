using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

public static partial class ProcurementSeeder
{
    private static Procurement BuildProcurement(
        ProcurementSeedData seed,
        ProcurementSeedUsers users,
        IReadOnlyDictionary<string, int> statuses,
        ProcurementSeedJobTypes jobTypes,
        Func<string> nextProcNum
    )
    {
        var jobType = ResolveJobType(seed.JobTypeName, jobTypes);
        var procurement = new Procurement
        {
            UserId = users.CreatedBy.Id,
            ProcNum = nextProcNum(),
            StatusId = statuses[seed.StatusName],
            JobTypeId = jobType.JobTypeId,
            ContractType = seed.ContractType,
            JobName = seed.JobName,
            SpkNumber = seed.SpkNumber,
            Wonum = seed.Wonum,
            StartDate = seed.StartDate,
            EndDate = seed.EndDate,
            ProjectRegion = seed.ProjectRegion,
            PotentialAccrualDate = seed.PotentialAccrualDate,
            SpmpNumber = seed.SpmpNumber,
            MemoNumber = seed.MemoNumber,
            OeNumber = seed.OeNumber,
            RaNumber = seed.RaNumber,
            ProjectCode = seed.ProjectCode,
            LtcName = seed.LtcName,
            Note = seed.Note,
            ProcurementCategory = seed.ProcurementCategory,
            PicOpsUserId = users.PicOpsUser.Id,
            AnalystHteUserId = users.AnalystUser.Id,
            AssistantManagerUserId = users.AssistantManagerUser.Id,
            ManagerUserId = users.ManagerUser.Id,
            CreatedAt = seed.CreatedAt,
        };

        procurement.ProcDetails = seed
            .Details.Select(detail => new ProcDetail
            {
                ItemName = detail.ItemName,
                Quantity = detail.Quantity,
                Unit = detail.Unit,
                DetailKind = "KEBUTUHAN_UNIT",
                ProcurementId = procurement.ProcurementId,
            })
            .ToList();

        procurement.ProcOffers = BuildProcurementOffers(seed, procurement.ProcurementId);
        return procurement;
    }

    private static JobTypes ResolveJobType(string name, ProcurementSeedJobTypes jobTypes) =>
        name switch
        {
            "Angkutan" => jobTypes.Angkutan,
            "StandBy" => jobTypes.StandBy,
            "Moving" => jobTypes.Moving,
            _ => throw new Exception($"Unknown JobType: {name}")
        };

    private static List<ProcOffer> BuildProcurementOffers(
        ProcurementSeedData seed,
        string procurementId
    ) =>
        new()
        {
            new()
            {
                ItemPenawaran = seed.JobTypeName == "Angkutan" ? "Truck HighBed" :
                               seed.JobTypeName == "StandBy" ? "Equipment Rental" :
                               "Transport Service",
                Qty = 1,
                Unit = "Buah",
                UnitRevenue = seed.JobTypeName == "Angkutan" ? "TRIP" :
                             seed.JobTypeName == "StandBy" ? "HARI" :
                             "TRIP",
                ProcurementId = procurementId,
            },
            new()
            {
                ItemPenawaran = seed.JobTypeName == "Angkutan" ? "Support Vehicle" :
                               seed.JobTypeName == "StandBy" ? "Backup Equipment" :
                               "Auxiliary Support",
                Qty = 1,
                Unit = "Pcs",
                UnitRevenue = seed.JobTypeName == "Angkutan" ? "TRIP" :
                             seed.JobTypeName == "StandBy" ? "JAM" :
                             "KALI",
                ProcurementId = procurementId,
            }
        };
}
