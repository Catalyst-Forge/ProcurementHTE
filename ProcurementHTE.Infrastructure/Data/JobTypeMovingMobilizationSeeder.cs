using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using System.Linq;
using System.Reflection.Metadata;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class JobTypeMovingMobilizationSeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            // === 0) TRANSAKSI (opsional tapi aman)
            using var tx = await context.Database.BeginTransactionAsync();

            // === 1) Pastikan JobType ada (lookup by TypeName)
            var typeName = "Moving & Mobilization";
            var jobType = await context.JobTypes.FirstOrDefaultAsync(t => t.TypeName == typeName);
            if (jobType == null)
            {
                jobType = new JobTypes
                {
                    TypeName = typeName,
                    Description = "Konfigurasi dokumen untuk proses Moving & Mobilization"
                };
                context.JobTypes.Add(jobType);
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

            // === 3) Konfigurasi JobTypeDocuments (idempotent, lookup by (JobTypeId, DocumentTypeId))
            var cfg = new (string Name, int Seq, bool Mandatory, bool Generated, bool UploadReq, bool RequiresApproval, string? Note)[]
            {
                ("Memorandum",                                   1,  true,  false, true,  true,  "Memorandum upload dengan approval Manager"),
                ("Permintaan Pekerjaan",                         2,  true,  false, true,  false, "Dokumen eksternal; tidak di-generate; tidak perlu approval"),
                ("Service Order",                                3,  true,  false, true,  true,  "Service Order upload dengan approval Manager"),
                ("Market Survey",                                4,  true,  false, true,  false, "Upload dari HTE; mempengaruhi progress"),
                ("Profit & Loss",                                5,  true,  true,  false, true,  "Digenerate sistem; approval Analyst HTE & LTS -> AM HTE -> Manager"),
                ("Surat Perintah Mulai Pekerjaan (SPMP)",        6,  true,  false, true,  true,  "SPMP upload dengan approval Manager"),
                ("Surat Penawaran Harga",                        7,  true,  false, true,  false, "Upload dari HTE"),
                ("Surat Negosiasi Harga",                        8,  true,  false, true,  false, "Upload dari HTE"),
                ("Rencana Kerja dan Syarat-Syarat (RKS)",        9,  true,  false, true, true,  "Digenerate sistem; approval AM HTE -> Manager"),
                ("Risk Assessment (RA)",                        10,  true,  false, true,  true,  "Upload (khusus pengadaan jasa); approval HSE -> AM HTE -> Manager"),
                ("Owner Estimate (OE)",                         11,  true,  false,  true, true,  "Digenerate sistem; approval AM HTE -> Manager"),
                ("Bill of Quantity (BOQ)",                      12,  true,  false, true, true, "BOQ digenerate otomatis oleh sistem")
            };

            // DbSet bisa bernama JobTypesDocuments (lihat log EF). Untuk aman gunakan Set<JobTypeDocuments>()
            var wtdSet = context.Set<JobTypeDocuments>();

            foreach (var c in cfg)
            {
                var dt = DT(c.Name);
                bool exists = await wtdSet.AnyAsync(x =>
                    x.JobTypeId == jobType.JobTypeId && x.DocumentTypeId == dt.DocumentTypeId);

                if (!exists)
                {
                    await wtdSet.AddAsync(new JobTypeDocuments
                    {
                        JobTypeId = jobType.JobTypeId,
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
                    .Where(x => x.JobTypeId == jobType.JobTypeId && x.DocumentType.Name == docName)
                    .FirstAsync();

                var roleId = await GetRoleIdOrNullAsync(roleName);
                if (roleId == null) return; // role belum ada, lewati saja

                bool exists = await context.DocumentApprovals.AnyAsync(a =>
                    a.JobTypeDocumentId == wtd.JobTypeDocumentId &&
                    a.RoleId == roleId &&
                    a.Level == level &&
                    a.SequenceOrder == seqOrder);

                if (!exists)
                {
                    context.DocumentApprovals.Add(new DocumentApprovals
                    {
                        JobTypeDocumentId = wtd.JobTypeDocumentId,
                        RoleId = roleId,
                        Level = level,
                        SequenceOrder = seqOrder
                    });
                }
            }

            var approvalMatrix = new (string Doc, (string Role, int Level, int Seq)[] Steps)[]
            {
                ("Memorandum", new[] {
                    ("Manager Transport & Logistic", 1, 1)
                }),
                ("Service Order", new[] {
                    ("Manager Transport & Logistic", 1, 1)
                }),
                ("Surat Perintah Mulai Pekerjaan (SPMP)", new[] {
                    ("Manager Transport & Logistic", 1, 1)
                }),
                ("Profit & Loss", new[] {
                    ("Analyst HTE & LTS", 1, 1),
                    ("Assistant Manager HTE", 2, 2),
                    ("Manager Transport & Logistic", 3, 3)
                }),
                ("Rencana Kerja dan Syarat-Syarat (RKS)", new[] {
                    ("Assistant Manager HTE", 1, 1),
                    ("Manager Transport & Logistic", 2, 2)
                }),
                ("Risk Assessment (RA)", new[] {
                    ("HSE", 1, 1),
                    ("Assistant Manager HTE", 2, 2),
                    ("Manager Transport & Logistic", 3, 3)
                }),
                ("Owner Estimate (OE)", new[] {
                    ("Assistant Manager HTE", 1, 1),
                    ("Manager Transport & Logistic", 2, 2)
                })
            };

            foreach (var (doc, steps) in approvalMatrix)
            {
                foreach (var (role, level, seq) in steps)
                {
                    await AddApprovalIfMissing(doc, role, level, seq);
                }
            }

            await context.SaveChangesAsync();
            await tx.CommitAsync();
        }
    }
}
