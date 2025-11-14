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

            var user =
                (
                    await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@example.com")
                    ?? await db.Users.FirstOrDefaultAsync()
                )
                ?? throw new Exception(
                    "Tidak ditemukan user di AspNetUsers. Jalankan Role/User seeder dulu."
                );

            var draftStatus =
                await db.Statuses.FirstOrDefaultAsync(s => s.StatusName == "Draft")
                ?? throw new Exception("Status 'Draft' belum ada. Jalankan Status seeder dulu.");

            var jobType =
                await db.JobTypes.FirstOrDefaultAsync(t => t.TypeName == "Moving & Mobilization")
                ?? throw new Exception(
                    "JobType 'Moving & Mobilization' belum ada. Jalankan JobTypeMovingMobilizationSeeder dulu."
                );

            var procNum = await GenerateNextProcNumAsync(db); // contoh: PROC000001

            var procurement = new Procurement
            {
                UserId = user.Id,
                ProcNum = procNum,
                StatusId = draftStatus.StatusId,
                JobTypeId = jobType.JobTypeId,
                JobType = jobType,
                ContractType = (ContractType)2,
                JobName = "Penyediaan jasa angkutan menggunakan trailer highbed untuk pengangkutan coring tools.",
                SpkNumber = "1063/PROC-DS/DSI1310/2025",
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2025, 10, 15),
                ProjectRegion = (ProjectRegion)1,
                AccrualAmount = 1_250_000_000m,
                RealizationAmount = 0,
                PotentialAccrualDate = new DateTime(2025, 11, 1),
                SpmpNumber = "SPMP-001/2025",
                MemoNumber = "MEMO-TRS/2025",
                OeNumber = "OE-TRS/2025",
                RaNumber = "RA-001/2025",
                ProjectCode = "HTE/2110/030",
                LtcName = "LTC Transport",
                Note = "Driver & kenek wajib menggunakan APD lengkap.",
                CreatedAt = DateTime.UtcNow,
            };

            db.Procurements.Add(procurement);
            await db.SaveChangesAsync(); // <-- penting: supaya PK Procurement terset

            // ==== INSERT ProcDetails (DETAIL) TANPA set PK identity ====
            var details = new List<ProcDetail> // gunakan nama entity detail kamu yang sesuai mapping tabel ProcDetails
            {
                new()
                {
                    ItemName = "Core Barrel (2 Jts 6-3/4\" Core Barrel dan 2 Jts 7\" Core Barrel) c/w protector",
                    Quantity = 4,
                    Unit = "Jts",
                    DetailKind = "KEBUTUHAN_UNIT",
                    ProcurementId = procurement.ProcurementId,
                },
                new()
                {
                    ItemName = "Lay Down Cradle",
                    Quantity = 2,
                    Unit = "Ea",
                    DetailKind = "KEBUTUHAN_UNIT",
                    ProcurementId = procurement.ProcurementId,
                },
                new()
                {
                    ItemName = "Inner Barrel Aluminium 4.3/4\" OD",
                    Quantity = 2,
                    Unit = "Jts",
                    DetailKind = "KEBUTUHAN_UNIT",
                    ProcurementId = procurement.ProcurementId
                },
                new()
                {
                    ItemName = "Float Sub 6.3/4\" c/w protector (dalam tool box)",
                    Quantity = 2,
                    Unit = "Ea",
                    DetailKind = "KEBUTUHAN_UNIT",
                    ProcurementId = procurement.ProcurementId
                },
                new()
                {
                    ItemName = "Tool box",
                    Quantity = 2,
                    Unit = "Ea",
                    DetailKind = "KEBUTUHAN_UNIT",
                    ProcurementId = procurement.ProcurementId
                },
                new()
                {
                    ItemName = "Marine Box (Dimensi 12,2 x 1,2 x 1 mtr)",
                    Quantity = 1,
                    Unit = "Ea",
                    DetailKind = "KEBUTUHAN_UNIT",
                    ProcurementId = procurement.ProcurementId
                },
                new()
                {
                    ItemName = "Pneumatic Air Saw",
                    Quantity = 1,
                    Unit = "Ea",
                    DetailKind = "KEBUTUHAN_UNIT",
                    ProcurementId = procurement.ProcurementId
                }
            };

            db.ProcDetails.AddRange(details);
            await db.SaveChangesAsync();
        }

        private static async Task<string> GenerateNextProcNumAsync(AppDbContext db)
        {
            const string prefix = "PROC";
            // Ambil ProcNum terakhir (urut desc)
            var last = await db
                .Procurements.Where(x => x.ProcNum!.StartsWith(prefix))
                .OrderByDescending(x => x.ProcNum)
                .Select(x => x.ProcNum)
                .FirstOrDefaultAsync();

            int next = 1;
            if (
                !string.IsNullOrWhiteSpace(last)
                && last.Length > 2
                && int.TryParse(last[2..], out var n)
            )
                next = n + 1;

            return $"{prefix}{next:D6}";
        }
    }
}

