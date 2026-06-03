using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data;

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
        var picOpsUser = await GetUserOrThrowAsync("pro.operation@example.com", "PIC Operation");
        var analystUser = await GetUserOrThrowAsync("AHte@example.com", "Analyst HTE & LTS");
        var assistantManagerUser = await GetUserOrThrowAsync(
            "assistantmanagerhte@example.com",
            "Assistant Manager HTE"
        );
        var managerUser = await GetUserOrThrowAsync(
            "manager@example.com",
            "Manager Transport & Logistic"
        );

        var requiredStatuses = new[] { "Draft", "Created", "In Progress", "Completed" };
        var statusLookup = await db
            .Statuses.Where(s => s.StatusName != null && requiredStatuses.Contains(s.StatusName))
            .ToDictionaryAsync(s => s.StatusName!, StringComparer.OrdinalIgnoreCase);

        foreach (var statusName in requiredStatuses)
        {
            if (!statusLookup.ContainsKey(statusName))
                throw new Exception($"Status '{statusName}' belum ada. Jalankan Status seeder dulu.");
        }

        // Get all JobTypes
        var angkutanJobType = await db.JobTypes.FirstOrDefaultAsync(t => t.TypeName == "Angkutan")
            ?? throw new Exception("JobType 'Angkutan' belum ada.");
        var standByJobType = await db.JobTypes.FirstOrDefaultAsync(t => t.TypeName == "StandBy")
            ?? throw new Exception("JobType 'StandBy' belum ada.");
        var movingJobType = await db.JobTypes.FirstOrDefaultAsync(t => t.TypeName == "Moving")
            ?? throw new Exception("JobType 'Moving' belum ada.");

        var sampleProcurements = new List<ProcurementSeedData>
        {
            // === ANGKUTAN - JASA ===
            new()
            {
                JobTypeName = "Angkutan",
                StatusName = "Draft",
                ContractType = ContractType.LTC,
                ProcurementCategory = ProcurementCategory.Jasa,
                JobName = "Jasa angkutan menggunakan trailer highbed untuk pengangkutan coring tools",
                SpkNumber = "1063/PROC-DS/DSI1310/2025",
                Wonum = "111/DSI1130/2025",
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2025, 10, 15),
                ProjectRegion = ProjectRegion.SMTR,
                PotentialAccrualDate = new DateTime(2025, 11, 1),
                SpmpNumber = "SPMP-001/2025",
                MemoNumber = "MEMO-ANG-JASA/2025",
                OeNumber = "OE-ANG-JASA/2025",
                RaNumber = "RA-001/2025",
                ProjectCode = "HTE/2110/030",
                LtcName = "LTC Transport",
                Note = "Driver & kenek wajib menggunakan APD lengkap.",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                Details =
                {
                    new("Core Barrel (2 Jts 6-3/4\" Core Barrel dan 2 Jts 7\" Core Barrel) c/w protector", 4, "Jts"),
                    new("Lay Down Cradle", 2, "Ea"),
                    new("Inner Barrel Aluminium 4.3/4\" OD", 2, "Jts"),
                    new("Float Sub 6.3/4\" c/w protector (dalam tool box)", 2, "Ea"),
                }
            },

            // === ANGKUTAN - BARANG ===
            new()
            {
                JobTypeName = "Angkutan",
                StatusName = "Draft",
                ContractType = ContractType.Spot,
                ProcurementCategory = ProcurementCategory.Barang,
                JobName = "Pengangkutan material konstruksi ke lokasi proyek Sumatra",
                SpkNumber = "1064/PROC-ANG/BAR001/2025",
                Wonum = "112/DSI1131/2025",
                StartDate = new DateTime(2025, 8, 1),
                EndDate = new DateTime(2025, 9, 30),
                ProjectRegion = ProjectRegion.JWKT,
                PotentialAccrualDate = new DateTime(2025, 10, 10),
                SpmpNumber = "SPMP-002/2025",
                MemoNumber = "MEMO-ANG-BAR/2025",
                OeNumber = "OE-ANG-BAR/2025",
                RaNumber = "RA-002/2025",
                ProjectCode = "HTE/2110/031",
                LtcName = "Material Transport",
                Note = "Material harus terlindung dari hujan selama pengangkutan",
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                Details =
                {
                    new("Semen Portland Type I", 100, "Zak"),
                    new("Besi Beton D16", 50, "Btg"),
                    new("Pasir Cor", 10, "M3"),
                    new("Batu Split 2/3", 8, "M3"),
                }
            },

            // === STANDBY - JASA ===
            new()
            {
                JobTypeName = "StandBy",
                StatusName = "Draft",
                ContractType = ContractType.RO,
                ProcurementCategory = ProcurementCategory.Jasa,
                JobName = "Sewa unit excavator untuk proyek pemboran geothermal",
                SpkNumber = "2001/PROC-STB/JSA001/2025",
                Wonum = "113/DSI1132/2025",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 12, 31),
                ProjectRegion = ProjectRegion.SMTR,
                PotentialAccrualDate = new DateTime(2026, 1, 15),
                SpmpNumber = "SPMP-003/2025",
                MemoNumber = "MEMO-STB-JASA/2025",
                OeNumber = "OE-STB-JASA/2025",
                RaNumber = "RA-003/2025",
                ProjectCode = "HTE/2110/032",
                LtcName = "Equipment Rental",
                Note = "Termasuk operator bersertifikat dan fuel",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                Details =
                {
                    new("Excavator Komatsu PC200", 1, "Pcs"),
                    new("Operator bersertifikat", 2, "Org"),
                    new("Fuel Support", 1, "Ls"),
                    new("Maintenance Kit", 1, "Set"),
                }
            },

            // === STANDBY - BARANG ===
            new()
            {
                JobTypeName = "StandBy",
                StatusName = "Draft",
                ContractType = ContractType.STB,
                ProcurementCategory = ProcurementCategory.Barang,
                JobName = "Sewa generator standby untuk base camp operasional",
                SpkNumber = "2002/PROC-STB/BAR001/2025",
                Wonum = "114/DSI1133/2025",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 11, 30),
                ProjectRegion = ProjectRegion.AKOMODASI,
                PotentialAccrualDate = new DateTime(2025, 12, 15),
                SpmpNumber = "SPMP-004/2025",
                MemoNumber = "MEMO-STB-BAR/2025",
                OeNumber = "OE-STB-BAR/2025",
                RaNumber = "RA-004/2025",
                ProjectCode = "HTE/2110/033",
                LtcName = "Power Support",
                Note = "Termasuk instalasi dan kabel power",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                Details =
                {
                    new("Generator 150 KVA", 2, "Pcs"),
                    new("Kabel Power 50m", 4, "Roll"),
                    new("Panel Distribution Board", 2, "Pcs"),
                    new("Fuel Tank 1000L", 2, "Pcs"),
                }
            },

            // === MOVING - JASA ===
            new()
            {
                JobTypeName = "Moving",
                StatusName = "Draft",
                ContractType = ContractType.LTC,
                ProcurementCategory = ProcurementCategory.Jasa,
                JobName = "Jasa mobilisasi rig dan demobilisasi dari lokasi Jambi ke Riau",
                SpkNumber = "3001/PROC-MOV/JSA001/2025",
                Wonum = "115/DSI1134/2025",
                StartDate = new DateTime(2025, 5, 1),
                EndDate = new DateTime(2025, 7, 31),
                ProjectRegion = ProjectRegion.SMTR,
                PotentialAccrualDate = new DateTime(2025, 8, 15),
                SpmpNumber = "SPMP-005/2025",
                MemoNumber = "MEMO-MOV-JASA/2025",
                OeNumber = "OE-MOV-JASA/2025",
                RaNumber = "RA-005/2025",
                ProjectCode = "HTE/2110/034",
                LtcName = "Rig Mobilization",
                Note = "Termasuk escort dan izin jalan untuk heavy equipment",
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                Details =
                {
                    new("Rig Mast Transportation", 1, "Ls"),
                    new("Substructure Transport", 1, "Ls"),
                    new("Power System Transport", 1, "Ls"),
                    new("Pipe Rack Transport", 1, "Ls"),
                }
            },

            // === MOVING - BARANG ===
            new()
            {
                JobTypeName = "Moving",
                StatusName = "Draft",
                ContractType = ContractType.Spot,
                ProcurementCategory = ProcurementCategory.Barang,
                JobName = "Mobilisasi peralatan safety dan emergency equipment",
                SpkNumber = "3002/PROC-MOV/BAR001/2025",
                Wonum = "116/DSI1135/2025",
                StartDate = new DateTime(2025, 10, 1),
                EndDate = new DateTime(2025, 11, 15),
                ProjectRegion = ProjectRegion.JWKT,
                PotentialAccrualDate = new DateTime(2025, 11, 30),
                SpmpNumber = "SPMP-006/2025",
                MemoNumber = "MEMO-MOV-BAR/2025",
                OeNumber = "OE-MOV-BAR/2025",
                RaNumber = "RA-006/2025",
                ProjectCode = "HTE/2110/035",
                LtcName = "Safety Equipment",
                Note = "Prioritas tinggi - untuk safety training karyawan baru",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Details =
                {
                    new("Fire Extinguisher CO2 9kg", 20, "Pcs"),
                    new("Emergency Stretcher", 4, "Pcs"),
                    new("First Aid Kit Complete", 10, "Set"),
                    new("Emergency Light", 15, "Pcs"),
                }
            },
        };

        var nextSequence = await GetNextProcurementSequenceAsync(db);
        string NextProcNum() => $"PROC{nextSequence++:D6}";

        var procurementEntities = new List<Procurement>();

        foreach (var seed in sampleProcurements)
        {
            var jobType = seed.JobTypeName switch
            {
                "Angkutan" => angkutanJobType,
                "StandBy" => standByJobType,
                "Moving" => movingJobType,
                _ => throw new Exception($"Unknown JobType: {seed.JobTypeName}")
            };

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

            // Unit of Items - free text (avoid UnitType codes: HARI, JAM, LSP, KM, KALI, TRIP)
            procurement.ProcOffers = new List<ProcOffer>
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
                    ProcurementId = procurement.ProcurementId,
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
                    ProcurementId = procurement.ProcurementId,
                }
            };

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
