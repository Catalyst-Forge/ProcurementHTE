using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using System.Reflection.Metadata;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class WoTypeMovingMobilizationSeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            // === 0) TRANSAKSI (opsional tapi aman)
            using var tx = await context.Database.BeginTransactionAsync();

            // === 1) Pastikan WoType ada (lookup by TypeName)
            var typeName = "Moving & Mobilization";
            var woType = await context.WoTypes.FirstOrDefaultAsync(t => t.TypeName == typeName);
            if (woType == null)
            {
                woType = new WoTypes
                {
                    TypeName = typeName,
                    Description = "Konfigurasi dokumen untuk proses Moving & Mobilization"
                };
                context.WoTypes.Add(woType);
                await context.SaveChangesAsync();
            }

            // === 2) Pastikan DocumentTypes yang diperlukan ada (lookup by Name)
            var docNames = new[]
            {
                "Memorandum",
                "Permintaan Pekerjaan",
                "Service Order",
                "Market Survey",
                "Profit & Loss",
                "Surat Perintah Mulai Pekerjaan (SPMP)",
                "Surat Penawaran Harga",
                "Surat Negosiasi Harga",
                "Rencana Kerja dan Syarat-Syarat (RKS)",
                "Risk Assessment (RA)",
                "Owner Estimate (OE)",
                "Bill of Quantity (BOQ)"
            };

            var existingDocs = await context.DocumentTypes
                .Where(d => docNames.Contains(d.Name))
                .ToListAsync();

            // Tambah yang belum ada
            foreach (var name in docNames.Except(existingDocs.Select(d => d.Name)))
            {
                var dt = new DocumentType { Name = name, Description = name };
                context.DocumentTypes.Add(dt);
                existingDocs.Add(dt);
            }
            await context.SaveChangesAsync();

            // Helper: ambil DocumentType by Name
            DocumentType DT(string name) =>
                existingDocs.First(d => d.Name == name);

            // === 3) Konfigurasi WoTypeDocuments (idempotent, lookup by (WoTypeId, DocumentTypeId))
            var cfg = new (string Name, int Seq, bool Mandatory, bool Generated, bool UploadReq, bool RequiresApproval, string? Note)[]
            {
                ("Memorandum",                                   1,  true,  false, true,  true,  "Memorandum approval Manager Transport & Logistic"),
                ("Permintaan Pekerjaan",                         2,  true,  false, true,  false, "Dokumen eksternal; tidak di-generate; tidak perlu approval"),
                ("Service Order",                                3,  true,  false, true,  true,  "Service Order approval Manager Transport & Logistic"),
                ("Market Survey",                                4,  true,  false, true,  false, "Market Survey upload dari HTE"),
                ("Profit & Loss",                                5,  true,  true,  false, true,  "Generate; approval berjenjang"),
                ("Surat Perintah Mulai Pekerjaan (SPMP)",        6,  true,  false, true,  true,  "SPMP approval Manager Transport & Logistic"),
                ("Surat Penawaran Harga",                        7,  true,  false, true,  false, "SPH upload dari HTE"),
                ("Surat Negosiasi Harga",                        8,  true,  false, true,  false, "SNH upload dari HTE"),
                ("Rencana Kerja dan Syarat-Syarat (RKS)",        9,  true,  true,  true, true,  "RKS generate; approval berjenjang"),
                ("Risk Assessment (RA)",                        10,  true,  false, true,  true,  "RA approval HSE -> AM HTE -> Manager T&L"),
                ("Owner Estimate (OE)",                         11,  true,  true,  true, true,  "OE generate; approval berjenjang"),
                ("Bill of Quantity (BOQ)",                      12,  true,  true,  true, false, "BOQ generate otomatis oleh sistem")
            };

            // DbSet bisa bernama WoTypesDocuments (lihat log EF). Untuk aman gunakan Set<WoTypeDocuments>()
            var wtdSet = context.Set<WoTypeDocuments>();

            foreach (var c in cfg)
            {
                var dt = DT(c.Name);
                bool exists = await wtdSet.AnyAsync(x =>
                    x.WoTypeId == woType.WoTypeId && x.DocumentTypeId == dt.DocumentTypeId);

                if (!exists)
                {
                    await wtdSet.AddAsync(new WoTypeDocuments
                    {
                        WoTypeId = woType.WoTypeId,
                        DocumentTypeId = dt.DocumentTypeId,
                        Sequence = c.Seq,
                        IsMandatory = c.Mandatory,
                        IsGenerated = c.Generated,
                        IsUploadRequired = c.UploadReq,
                        RequiresApproval = c.RequiresApproval,
                        Note = c.Note
                    });
                }
            }
            await context.SaveChangesAsync();

            // === 4) Approvals (tambahkan hanya jika ada Role-nya & belum ada barisnya)
            async Task<string?> GetRoleIdOrNullAsync(string roleName)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                return role?.Id; // jangan melempar; biarkan skip kalau role belum ada
            }

            async Task AddApprovalIfMissing(string docName, string roleName, int level, int seqOrder)
            {
                var wtd = await wtdSet
                    .Include(x => x.DocumentType)
                    .Where(x => x.WoTypeId == woType.WoTypeId && x.DocumentType.Name == docName)
                    .FirstAsync();

                var roleId = await GetRoleIdOrNullAsync(roleName);
                if (roleId == null) return; // role belum ada, lewati saja

                bool exists = await context.DocumentApprovals.AnyAsync(a =>
                    a.WoTypeDocumentId == wtd.WoTypeDocumentId &&
                    a.RoleId == roleId &&
                    a.Level == level &&
                    a.SequenceOrder == seqOrder);

                if (!exists)
                {
                    context.DocumentApprovals.Add(new DocumentApprovals
                    {
                        WoTypeDocumentId = wtd.WoTypeDocumentId,
                        RoleId = roleId,
                        Level = level,
                        SequenceOrder = seqOrder
                    });
                }
            }

            // contoh minimal sesuai yang kamu tulis
            await AddApprovalIfMissing("Memorandum",                                "HSE", 1, 1);
            await AddApprovalIfMissing("Service Order",                             "HSE", 1, 1);
            await AddApprovalIfMissing("Surat Perintah Mulai Pekerjaan (SPMP)",     "HSE", 1, 1);
            await AddApprovalIfMissing("Profit & Loss",                             "HSE", 1, 1);
            await AddApprovalIfMissing("Rencana Kerja dan Syarat-Syarat (RKS)",     "HSE", 1, 1);
            await AddApprovalIfMissing("Risk Assessment (RA)",                      "HSE", 1, 1);
            await AddApprovalIfMissing("Owner Estimate (OE)",                       "HSE", 1, 1);

            await AddApprovalIfMissing("Memorandum",                                "Assistant Manager HTE", 2, 2);
            await AddApprovalIfMissing("Service Order",                             "Assistant Manager HTE", 2, 2);
            await AddApprovalIfMissing("Surat Perintah Mulai Pekerjaan (SPMP)",     "Assistant Manager HTE", 2, 2);
            await AddApprovalIfMissing("Profit & Loss",                             "Assistant Manager HTE", 2, 2);
            await AddApprovalIfMissing("Rencana Kerja dan Syarat-Syarat (RKS)",     "Assistant Manager HTE", 2, 2);
            await AddApprovalIfMissing("Risk Assessment (RA)",                      "Assistant Manager HTE", 2, 2);
            await AddApprovalIfMissing("Owner Estimate (OE)",                       "Assistant Manager HTE", 2, 2);

            await AddApprovalIfMissing("Memorandum",                                "Manager Transport & Logistic", 3, 3);
            await AddApprovalIfMissing("Service Order",                             "Manager Transport & Logistic", 3, 3);
            await AddApprovalIfMissing("Surat Perintah Mulai Pekerjaan (SPMP)",     "Manager Transport & Logistic", 3, 3);
            await AddApprovalIfMissing("Profit & Loss",                             "Manager Transport & Logistic", 3, 3);
            await AddApprovalIfMissing("Rencana Kerja dan Syarat-Syarat (RKS)",     "Manager Transport & Logistic", 3, 3);
            await AddApprovalIfMissing("Risk Assessment (RA)",                      "Manager Transport & Logistic", 3, 3);
            await AddApprovalIfMissing("Owner Estimate (OE)",                       "Manager Transport & Logistic", 3, 3);

            await AddApprovalIfMissing("Memorandum",                                "Vice President", 4, 4);
            await AddApprovalIfMissing("Service Order",                             "Vice President", 4, 4);
            await AddApprovalIfMissing("Surat Perintah Mulai Pekerjaan (SPMP)",     "Vice President", 4, 4);
            await AddApprovalIfMissing("Profit & Loss",                             "Vice President", 4, 4);
            await AddApprovalIfMissing("Rencana Kerja dan Syarat-Syarat (RKS)",     "Vice President", 4, 4);
            await AddApprovalIfMissing("Risk Assessment (RA)",                      "Vice President", 4, 4);
            await AddApprovalIfMissing("Owner Estimate (OE)",                       "Vice President", 4, 4);

            await context.SaveChangesAsync();
            await tx.CommitAsync();
        }
    }
}
