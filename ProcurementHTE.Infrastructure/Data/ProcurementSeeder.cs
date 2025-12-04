using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class ProcurementSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.Procurements.AnyAsync())
                return;

            async Task<User> GetUserOrThrowAsync(string email, string description)
            {
                return await db.Users.FirstOrDefaultAsync(u => u.Email == email)
                    ?? throw new Exception(
                        $"User '{description}' ({email}) belum ada. Jalankan Role/User seeder dulu."
                    );
            }

            var createdBy = await GetUserOrThrowAsync("admin@example.com", "Admin");
            var picOpsUser = await GetUserOrThrowAsync(
                "pro.operation@example.com",
                "PIC Operation"
            );
            var analystUser = await GetUserOrThrowAsync("AHte@example.com", "Analyst HTE & LTS");
            var assistantManagerUser = await GetUserOrThrowAsync(
                "assistantmanagerhte@example.com",
                "Assistant Manager HTE"
            );
            var managerUser = await GetUserOrThrowAsync(
                "manager@example.com",
                "Manager Transport & Logistic"
            );

            var requiredStatuses = new[]
            {
                "Draft",
                "Created",
                "In Progress",
                "Uploaded",
                "Completed",
            };
            var statusLookup = await db
                .Statuses.Where(s =>
                    s.StatusName != null && requiredStatuses.Contains(s.StatusName)
                )
                .ToDictionaryAsync(s => s.StatusName!, StringComparer.OrdinalIgnoreCase);

            foreach (var statusName in requiredStatuses)
            {
                if (!statusLookup.ContainsKey(statusName))
                    throw new Exception(
                        $"Status '{statusName}' belum ada. Jalankan Status seeder dulu."
                    );
            }

            var jobType =
                await db.JobTypes.FirstOrDefaultAsync(t => t.TypeName == "Moving & Mobilization")
                ?? await db.JobTypes.FirstOrDefaultAsync(t => t.TypeName == "Moving")
                ?? throw new Exception(
                    "JobType 'Moving' belum ada. Jalankan JobTypeMovingMobilizationSeeder dulu."
                );

            var sampleProcurements = new List<ProcurementSeedData>
            {
                new()
                {
                    StatusName = "Draft",
                    ContractType = ContractType.LTC,
                    ProcurementCategory = ProcurementCategory.Services,
                    JobName =
                        "Penyediaan jasa angkutan menggunakan trailer highbed untuk pengangkutan coring tools.",
                    SpkNumber = "1063/PROC-DS/DSI1310/2025",
                    Wonum = "111 / DSI1130/2025",
                    StartDate = new DateTime(2025, 9, 1),
                    EndDate = new DateTime(2025, 10, 15),
                    ProjectRegion = ProjectRegion.SMRT,
                    PotentialAccrualDate = new DateTime(2025, 11, 1),
                    SpmpNumber = "SPMP-001/2025",
                    MemoNumber = "MEMO-TRS/2025",
                    OeNumber = "OE-TRS/2025",
                    RaNumber = "RA-001/2025",
                    ProjectCode = "HTE/2110/030",
                    LtcName = "LTC Transport",
                    Note = "Driver & kenek wajib menggunakan APD lengkap.",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    Details =
                    {
                        new(
                            "Core Barrel (2 Jts 6-3/4\" Core Barrel dan 2 Jts 7\" Core Barrel) c/w protector",
                            4,
                            "Jts"
                        ),
                        new("Lay Down Cradle", 2, "Ea"),
                        new("Inner Barrel Aluminium 4.3/4\" OD", 2, "Jts"),
                        new("Float Sub 6.3/4\" c/w protector (dalam tool box)", 2, "Ea"),
                        new("Tool box", 2, "Ea"),
                        new("Marine Box (Dimensi 12,2 x 1,2 x 1 mtr)", 1, "Ea"),
                        new("Pneumatic Air Saw", 1, "Ea"),
                    },
                },
                new()
                {
                    StatusName = "Created",
                    ContractType = ContractType.Spot,
                    ProcurementCategory = ProcurementCategory.Goods,
                    JobName = "Pengadaan chemical injection skid untuk sumur WKP Kamojang.",
                    SpkNumber = "221/PROC-CHM/CHEM45/2025",
                    Wonum = "112 / DSI1131/2025",
                    StartDate = new DateTime(2025, 7, 5),
                    EndDate = new DateTime(2025, 9, 20),
                    ProjectRegion = ProjectRegion.JWKT,
                    PotentialAccrualDate = new DateTime(2025, 9, 30),
                    SpmpNumber = "SPMP-014/2025",
                    MemoNumber = "MEMO-CHM/2025",
                    OeNumber = "OE-CHM/2025",
                    RaNumber = "RA-014/2025",
                    ProjectCode = "HTE/2107/014",
                    LtcName = "Chemical Services",
                    Note = "Fokuskan pada pengiriman aman untuk bahan kimia berbahaya.",
                    CreatedAt = DateTime.UtcNow.AddDays(-45),
                    Details =
                    {
                        new("Chemical Injection Pump 10K PSI", 3, "Unit"),
                        new("Portable Fuel Tank 5.000 L", 2, "Unit"),
                        new("Calibrated Pressure Gauge 10K PSI", 6, "Ea"),
                        new("Spill Kit untuk handling chemical", 4, "Set"),
                    },
                },
                new()
                {
                    StatusName = "In Progress",
                    ContractType = ContractType.RO,
                    JobName =
                        "Support mobilisasi rig dan kru untuk proyek geothermal South Sumatra.",
                    SpkNumber = "455/PROC-RIG/RIG200/2025",
                    Wonum = "113 / DSI1132/2025",
                    StartDate = new DateTime(2025, 10, 1),
                    EndDate = new DateTime(2025, 12, 5),
                    ProjectRegion = ProjectRegion.AKOMODASI,
                    PotentialAccrualDate = new DateTime(2025, 12, 10),
                    SpmpNumber = "SPMP-030/2025",
                    MemoNumber = "MEMO-RIG/2025",
                    OeNumber = "OE-RIG/2025",
                    RaNumber = "RA-030/2025",
                    ProjectCode = "HTE/2110/055",
                    LtcName = "Rig Support Services",
                    Note = "Termasuk dukungan akomodasi dan transportasi kru malam hari.",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    Details =
                    {
                        new("Crew Transportation (Jakarta - Riau PP)", 12, "Trip"),
                        new("Rig Up Support Team", 1, "Lot"),
                        new("Night Shift Tools Storage", 2, "Set"),
                        new("Emergency Generator Rental 150 KVA", 1, "Unit"),
                    },
                },
                new()
                {
                    StatusName = "Uploaded",
                    ContractType = ContractType.STB,
                    JobName = "Distribusi peralatan safety ke site Kalimantan Timur.",
                    SpkNumber = "588/PROC-HSE/HSE180/2025",
                    Wonum = "114 / DSI1133/2025",
                    StartDate = new DateTime(2025, 8, 10),
                    EndDate = new DateTime(2025, 9, 5),
                    ProjectRegion = ProjectRegion.SMRT,
                    PotentialAccrualDate = new DateTime(2025, 9, 15),
                    SpmpNumber = "SPMP-020/2025",
                    MemoNumber = "MEMO-HSE/2025",
                    OeNumber = "OE-HSE/2025",
                    RaNumber = "RA-020/2025",
                    ProjectCode = "HTE/2108/020",
                    LtcName = "HSE Logistics",
                    Note = "Mengutamakan pengiriman tepat waktu karena jadwal training safety.",
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    Details =
                    {
                        new("Helm Safety SNI", 50, "Unit"),
                        new("Coverall tahan api", 40, "Unit"),
                        new("Safety Shoes", 40, "Pasang"),
                        new("Gas Detector Portable", 6, "Unit"),
                    },
                },
                new()
                {
                    StatusName = "Completed",
                    ContractType = ContractType.RO,
                    JobName = "Perawatan armada trailer untuk proyek Sumatra Selatan.",
                    SpkNumber = "701/PROC-MTN/MTN250/2025",
                    Wonum = "115 / DSI1134/2025",
                    StartDate = new DateTime(2025, 5, 1),
                    EndDate = new DateTime(2025, 7, 30),
                    ProjectRegion = ProjectRegion.AKOMODASI,
                    PotentialAccrualDate = new DateTime(2025, 8, 10),
                    SpmpNumber = "SPMP-045/2025",
                    MemoNumber = "MEMO-MTN/2025",
                    OeNumber = "OE-MTN/2025",
                    RaNumber = "RA-045/2025",
                    ProjectCode = "HTE/2105/045",
                    LtcName = "Maintenance Services",
                    Note = "Meliputi pengecekan mesin, rem, dan pergantian ban cadangan.",
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    Details =
                    {
                        new("Overhaul mesin trailer", 5, "Unit"),
                        new("Penggantian ban trailer 24\"", 20, "Pc"),
                        new("Service sistem pengereman", 5, "Unit"),
                        new("Kalibrasi torque wrench heavy duty", 3, "Unit"),
                    },
                },
            };

            var nextSequence = await GetNextProcurementSequenceAsync(db);
            string NextProcNum() => $"PROC{nextSequence++:D6}";

            var procurementEntities = new List<Procurement>();

            foreach (var seed in sampleProcurements)
            {
                var procurement = new Procurement
                {
                    UserId = createdBy.Id,
                    ProcNum = NextProcNum(),
                    StatusId = statusLookup[seed.StatusName].StatusId,
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
                    PicOpsUserId = picOpsUser.Id,
                    AnalystHteUserId = analystUser.Id,
                    AssistantManagerUserId = assistantManagerUser.Id,
                    ManagerUserId = managerUser.Id,
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

                procurement.ProcOffers =
                [
                    new ProcOffer
                    {
                        ItemPenawaran = "HighBed",
                        Qty = 1,
                        Unit = "Unit",
                        ProcurementId = procurement.ProcurementId,
                    },
                    new ProcOffer
                    {
                        ItemPenawaran = "LightTruck",
                        Qty = 1,
                        Unit = "Unit",
                        ProcurementId = procurement.ProcurementId,
                    },
                ];

                procurementEntities.Add(procurement);
            }

            db.Procurements.AddRange(procurementEntities);
            await db.SaveChangesAsync();
        }

        private static async Task<int> GetNextProcurementSequenceAsync(AppDbContext db)
        {
            const string prefix = "PROC";
            var last = await db
                .Procurements.Where(x => x.ProcNum != null && x.ProcNum.StartsWith(prefix))
                .OrderByDescending(x => x.ProcNum)
                .Select(x => x.ProcNum)
                .FirstOrDefaultAsync();

            if (
                !string.IsNullOrWhiteSpace(last)
                && last.Length > prefix.Length
                && int.TryParse(last[prefix.Length..], out var current)
            )
                return current + 1;

            return 1;
        }

        private sealed record ProcurementSeedData
        {
            public string StatusName { get; init; } = null!;
            public ContractType ContractType { get; init; }
            public string JobName { get; init; } = null!;
            public ProcurementCategory ProcurementCategory { get; init; } = ProcurementCategory.Goods;
            public string SpkNumber { get; init; } = null!;
            public string Wonum { get; init; } = null!;
            public DateTime StartDate { get; init; }
            public DateTime EndDate { get; init; }
            public ProjectRegion ProjectRegion { get; init; }
            public DateTime PotentialAccrualDate { get; init; }
            public string SpmpNumber { get; init; } = null!;
            public string MemoNumber { get; init; } = null!;
            public string OeNumber { get; init; } = null!;
            public string RaNumber { get; init; } = null!;
            public string ProjectCode { get; init; } = null!;
            public string LtcName { get; init; } = null!;
            public string Note { get; init; } = null!;
            public DateTime CreatedAt { get; init; }
            public List<ProcDetailSeed> Details { get; init; } = new();
        }

        private sealed record ProcDetailSeed(string ItemName, int Quantity, string Unit);
    }
}
