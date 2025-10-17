using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class WorkOrderSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.WorkOrders.AnyAsync())
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

            var woType =
                await db.WoTypes.FirstOrDefaultAsync(t => t.TypeName == "Moving & Mobilization")
                ?? throw new Exception(
                    "WoType 'Moving & Mobilization' belum ada. Jalankan WoTypeMovingMobilizationSeeder dulu."
                );

            var woNum = await GenerateNextWoNumAsync(db); // contoh: WO000001

            var wo = new WorkOrder
            {
                UserId = user.Id, // <- FK valid ke AspNetUsers
                WoNum = "WO000001",
                StatusId = 1, // <- FK valid ke Statuses
                ProcurementType = (ProcurementType)2, // contoh: jasa
                WoTypeId = woType.WoTypeId, // FK ke WoTypes

                WoLetter = "PERMINTAAN PEKERJAAN",
                DateLetter = new DateTime(2025, 9, 1),
                Description =
                    "Penyediaan jasa angkutan menggunakan trailer highbed untuk pengangkutan coring tools.",
                FromLocation = "Subsurface Services Manager",
                Destination = "Manager Transport & Logistic",
                WorkOrderLetter = null,
                WBS = null,
                GlAccount = "5005000130",
                DateRequired = new DateTime(2025, 8, 5),
                XS1 = "Lokasi: LBK-INF16, Rig PDSI #29.3/D1500-E, Lembak",
                XS2 = "Workshop PDSI Sunter",
                Note = "- Driver & Kenek dilengkapi PPE lengkap",
                XS3 = "CP Lokasi: Seto W - 081320602326",
                XS4 = "CP Workshop: Zainal Arifin - 085893668808",
                Requester = "Tito Ambardi J.",
                Approved = "I Made Trisna Mirawan",
                CreatedAt = DateTime.Now,
                FileWorkOrder = "permintaan-pekerjaan-2025.pdf",
                VendorId = null,
            };

            db.WorkOrders.Add(wo);
            await db.SaveChangesAsync(); // <-- penting: supaya PK WorkOrder terset

            // ==== INSERT WoDetails (DETAIL) TANPA set PK identity ====
            var details = new List<WoDetail> // gunakan nama entity detail kamu yang sesuai mapping tabel WoDetails
            {
                new()
                {
                    ItemName = "Core Barrel 6-3/4\" (c/w protector)",
                    Quantity = 2,
                    Unit = "Jts",
                    WorkOrderId = wo.WorkOrderId,
                },
                new()
                {
                    ItemName = "Lay Down Cradle",
                    Quantity = 2,
                    Unit = "Ea",
                    WorkOrderId = wo.WorkOrderId,
                },
                // tambahkan item lain sesuai kebutuhan
            };

            db.WoDetails.AddRange(details);
            await db.SaveChangesAsync();
        }

        private static async Task<string> GenerateNextWoNumAsync(AppDbContext db)
        {
            const string prefix = "WO";
            // Ambil WoNum terakhir (urut desc)
            var last = await db
                .WorkOrders.Where(x => x.WoNum!.StartsWith(prefix))
                .OrderByDescending(x => x.WoNum)
                .Select(x => x.WoNum)
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
