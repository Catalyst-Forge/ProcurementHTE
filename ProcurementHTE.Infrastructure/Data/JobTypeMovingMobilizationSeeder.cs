using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Infrastructure.Data
{
    public static class JobTypeMovingMobilizationSeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            // === 0) TRANSAKSI (opsional tapi aman)
            using var tx = await context.Database.BeginTransactionAsync();

            // === 1) Pastikan JobType ada (lookup by TypeName)
            var typeName = "Moving";
            var legacyTypeName = "Moving & Mobilization";
            var description = "Konfigurasi dokumen untuk proses Moving & Mobilization";

            var jobType = await context.JobTypes.FirstOrDefaultAsync(t => t.TypeName == typeName);

            // Bila data lama memakai nama berbeda, upgrade ke nama yang diinginkan
            if (jobType == null)
            {
                jobType = await context.JobTypes.FirstOrDefaultAsync(t =>
                    t.TypeName == legacyTypeName
                );
            }

            if (jobType == null)
            {
                jobType = new JobTypes
                {
                    TypeName = typeName,
                    Description = description,
                };
                context.JobTypes.Add(jobType);
                await context.SaveChangesAsync();
            }
            else
            {
                var updated = false;
                if (jobType.TypeName != typeName)
                {
                    jobType.TypeName = typeName;
                    updated = true;
                }
                if (jobType.Description != description)
                {
                    jobType.Description = description;
                    updated = true;
                }

                if (updated)
                {
                    context.JobTypes.Update(jobType);
                    await context.SaveChangesAsync();
                }
            }

            // === 2) Pastikan DocumentTypes yang diperlukan ada (lookup by Name)
            var docNames = new[]
            {
                "Memorandum",
                "Permintaan Pekerjaan",
                "PR Service",
                "Service Order",
                "Market Survey",
                "Profit & Loss",
                "Surat Perintah Mulai Pekerjaan (SPMP)",
                "Surat Penawaran Harga",
                "Surat Negosiasi Harga",
                "Rencana Kerja dan Syarat-Syarat (RKS)",
                "Risk Assessment (RA)",
                "Owner Estimate (OE)",
                "Bill of Quantity (BOQ)",
                "Justifikasi",
            };

            var existingDocs = await context
                .DocumentTypes.Where(d => docNames.Contains(d.Name))
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
            DocumentType DT(string name)
            {
                var dt = existingDocs.FirstOrDefault(d => d.Name == name);
                if (dt == null)
                {
                    dt = new DocumentType { Name = name, Description = name };
                    context.DocumentTypes.Add(dt);
                    existingDocs.Add(dt);
                }
                return dt;
            }

            // === 3) Konfigurasi JobTypeDocuments (idempotent, lookup by (JobTypeId, DocumentTypeId))
            var cfg = new (
                string Name,
                int Seq,
                bool Mandatory,
                bool Generated,
                bool UploadReq,
                bool RequiresApproval,
                string? Note,
                ProcurementCategory? Category
            )[]
            {
                (
                    "Memorandum",
                    1,
                    true,
                    true,
                    false,
                    true,
                    "Memorandum upload dengan approval Manager",
                    null
                ),
                (
                    "Permintaan Pekerjaan",
                    2,
                    true,
                    false,
                    true,
                    false,
                    "Dokumen eksternal; tidak di-generate; tidak perlu approval",
                    null
                ),
                (
                    "PR Service",
                    3,
                    true,
                    false,
                    true,
                    true,
                    "PR Service upload dengan approval Manager",
                    null // berlaku Goods & Services
                ),
                (
                    "Service Order",
                    4,
                    true,
                    true,
                    false,
                    true,
                    "Digenerate sistem; approval Manager Transport & Logistic",
                    null
                ),
                (
                    "Market Survey",
                    5,
                    true,
                    false,
                    true,
                    false,
                    "Upload dari HTE; mempengaruhi progress",
                    null
                ),
                (
                    "Profit & Loss",
                    6,
                    true,
                    true,
                    false,
                    true,
                    "Digenerate sistem; approval Analyst HTE & LTS -> AM HTE -> Manager",
                    null
                ),
                (
                    "Surat Perintah Mulai Pekerjaan (SPMP)",
                    7,
                    true,
                    true,
                    false,
                    true,
                    "SPMP upload dengan approval Manager",
                    null
                ),
                (
                    "Surat Penawaran Harga",
                    8,
                    true,
                    false,
                    true,
                    false,
                    "Upload dari HTE",
                    null
                ),
                ("Surat Negosiasi Harga", 9, true, false, true, false, "Upload dari HTE", null),
                (
                    "Rencana Kerja dan Syarat-Syarat (RKS)",
                    10,
                    true,
                    true,
                    false,
                    true,
                    "Digenerate sistem; approval AM HTE -> Manager",
                    ProcurementCategory.Services
                ),
                (
                    "Risk Assessment (RA)",
                    11,
                    true,
                    true,
                    false,
                    true,
                    "Upload (khusus pengadaan jasa); approval HSE -> AM HTE -> Manager",
                    ProcurementCategory.Services
                ),
                (
                    "Owner Estimate (OE)",
                    12,
                    true,
                    true,
                    false,
                    true,
                    "Digenerate sistem; approval AM HTE -> Manager",
                    null
                ),
                (
                    "Bill of Quantity (BOQ)",
                    13,
                    true,
                    true,
                    false,
                    true,
                    "BOQ digenerate otomatis oleh sistem",
                    null
                ),
                (
                    "Justifikasi",
                    14,
                    true,
                    true,
                    false,
                    true,
                    "Justifikasi (>=300jt, approval kondisional saat generate flow)",
                    null
                ),
            };

            // DbSet bisa bernama JobTypesDocuments (lihat log EF). Untuk aman gunakan Set<JobTypeDocuments>()
            var wtdSet = context.Set<JobTypeDocuments>();

            foreach (var c in cfg)
            {
                var dt = DT(c.Name);
                bool exists = await wtdSet.AnyAsync(x =>
                    x.JobTypeId == jobType.JobTypeId && x.DocumentTypeId == dt.DocumentTypeId
                );

                if (!exists)
                {
                    await wtdSet.AddAsync(
                        new JobTypeDocuments
                        {
                            JobTypeId = jobType.JobTypeId,
                            DocumentTypeId = dt.DocumentTypeId,
                            Sequence = c.Seq,
                            IsMandatory = c.Mandatory,
                            IsGenerated = c.Generated,
                            IsUploadRequired = c.UploadReq,
                            RequiresApproval = c.RequiresApproval,
                            Note = c.Note,
                            ProcurementCategory = c.Category,
                        }
                    );
                }
            }
            await context.SaveChangesAsync();

            // === 4) Approvals (tambahkan hanya jika ada Role-nya & belum ada barisnya)
            async Task<string?> GetRoleIdOrNullAsync(string roleName)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                return role?.Id; // jangan melempar; biarkan skip kalau role belum ada
            }

            async Task AddApprovalIfMissing(string docName, string roleName, int level)
            {
                var wtd = await wtdSet
                    .Include(x => x.DocumentType)
                    .Where(x => x.JobTypeId == jobType.JobTypeId && x.DocumentType.Name == docName)
                    .FirstAsync();

                var roleId = await GetRoleIdOrNullAsync(roleName);
                if (roleId == null)
                    return; // role belum ada, lewati saja

                bool exists = await context.DocumentApprovals.AnyAsync(a =>
                    a.JobTypeDocumentId == wtd.JobTypeDocumentId
                    && a.RoleId == roleId
                    && a.Level == level
                );

                if (!exists)
                {
                    context.DocumentApprovals.Add(
                        new DocumentApprovals
                        {
                            JobTypeDocumentId = wtd.JobTypeDocumentId,
                            RoleId = roleId,
                            Level = level,
                        }
                    );
                }
            }

            var approvalMatrix = new (string Doc, (string Role, int Level)[] Steps)[]
            {
                ("Memorandum", new[] { ("Manager Transport & Logistic", 1) }),
                ("Service Order", new[] { ("Manager Transport & Logistic", 1) }),
                (
                    "Surat Perintah Mulai Pekerjaan (SPMP)",
                    new[] { ("Manager Transport & Logistic", 1) }
                ),
                (
                    "Profit & Loss",
                    new[]
                    {
                        ("Analyst HTE & LTS", 1),
                        ("Assistant Manager HTE", 2),
                        ("Manager Transport & Logistic", 3),
                    }
                ),
                (
                    "Rencana Kerja dan Syarat-Syarat (RKS)",
                    new[]
                    {
                        ("Assistant Manager HTE", 1),
                        ("Manager Transport & Logistic", 2),
                    }
                ),
                (
                    "Risk Assessment (RA)",
                    new[]
                    {
                        ("HSE", 1),
                        ("Assistant Manager HTE", 2),
                        ("Manager Transport & Logistic", 3),
                    }
                ),
                (
                    "Owner Estimate (OE)",
                    new[]
                    {
                        ("Assistant Manager HTE", 1),
                        ("Manager Transport & Logistic", 2),
                    }
                ),
            };

            foreach (var (doc, steps) in approvalMatrix)
            {
                foreach (var (role, level) in steps)
                {
                    await AddApprovalIfMissing(doc, role, level);
                }
            }

            await context.SaveChangesAsync();
            await tx.CommitAsync();
        }
    }
}
