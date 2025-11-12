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

                WoNumLetter = "1063/WO-DS/DSI1310/2025",
                DateLetter = new DateTime(2025, 9, 1),
                Description =
                    "Penyediaan jasa angkutan menggunakan trailer highbed untuk pengangkutan coring tools.",
                From = "Subsurface Services Manager",
                To = "Manager Transport & Logistic",
                WorkOrderLetter = null,
                WBS = null,
                GlAccount = "5005000130",
                DateRequired = new DateTime(2025, 8, 5),
                XS1 = "LBK-INF16, Rig PDSI #29.3/D1500-E, Lembak",
                XS2 = "PDSI Sunter",
                Note = "- Driver & Kenek dilengkapi PPE lengkap (Safety Helmet, Safety Shoes, Kaca Mata Safety, dan Hand Gloves",
                XS3 = "Seto W - 081320602326",
                XS4 = "Zainal Arifin - 085893668808",
                Requester = "Tito Ambardi J.",
                Approved = "I Made Trisna Mirawan",
                CreatedAt = DateTime.Now,
                FileWorkOrder = "permintaan-pekerjaan-2025.pdf",
            };

            db.WorkOrders.Add(wo);
            await db.SaveChangesAsync(); // <-- penting: supaya PK WorkOrder terset

            // ==== INSERT WoDetails (DETAIL) TANPA set PK identity ====
            var details = new List<WoDetail> // gunakan nama entity detail kamu yang sesuai mapping tabel WoDetails
            {
                new()
                {
                    ItemName = "Core Barrel (2 Jts 6-3/4\" Core Barrel dan 2 Jts 7\" Core Barrel) c/w protector",
                    Quantity = 4,
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
                new()
                {
                    ItemName = "Inner Barrel Aluminium 4.3/4\" OD",
                    Quantity = 2,
                    Unit = "Jts",
                    WorkOrderId = wo.WorkOrderId
                },
                new()
                {
                    ItemName = "Float Sub 6.3/4\" c/w protector (dalam tool box)",
                    Quantity = 2,
                    Unit = "Ea",
                    WorkOrderId = wo.WorkOrderId
                },
                new()
                {
                    ItemName = "Tool box",
                    Quantity = 2,
                    Unit = "Ea",
                    WorkOrderId = wo.WorkOrderId
                },
                new()
                {
                    ItemName = "Marine Box (Dimensi 12,2 x 1,2 x 1 mtr)",
                    Quantity = 1,
                    Unit = "Ea",
                    WorkOrderId = wo.WorkOrderId
                },
                new()
                {
                    ItemName = "Pneumatic Air Saw",
                    Quantity = 1,
                    Unit = "Ea",
                    WorkOrderId = wo.WorkOrderId
                }
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
