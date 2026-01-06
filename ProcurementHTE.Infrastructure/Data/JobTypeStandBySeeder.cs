using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public class JobTypeStandBySeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            using var tx = await context.Database.BeginTransactionAsync();

            var typeName = "StandBy";
            var legacyTypeName = "Stand By";
            var description = "Konfigurasi dokumen untuk pengadaan Stand By (Jasa)";
            var jobType = await context.JobTypes.FirstOrDefaultAsync(t => t.TypeName == typeName);

            if (jobType == null)
            {
                jobType = await context.JobTypes.FirstOrDefaultAsync(t =>
                    t.TypeName == legacyTypeName
                );
            }

            if (jobType == null)
            {
                jobType = new JobTypes { TypeName = typeName, Description = description };
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

            var docNames = new[]
            {
                "Memorandum",
                "Permintaan Pekerjaan",
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

            foreach (var name in docNames.Except(existingDocs.Select(d => d.Name)))
            {
                var dt = new DocumentType { Name = name, Description = name };
                context.DocumentTypes.Add(dt);
                existingDocs.Add(dt);
            }

            await context.SaveChangesAsync();

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

            var configDocuments = new (
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
                    "Permintaan Pekerjaan",
                    1,
                    true,
                    false,
                    true,
                    false,
                    "Dokumen eksternal; tidak di-generate; tidak perlu approval",
                    null
                ),
                (
                    "Profit & Loss",
                    2,
                    true,
                    true,
                    false,
                    true,
                    "Di-generate sistem; approval Analyst HTE & LTS -> AM HTE -> Manager",
                    null
                ),
                (
                    "Surat Penawaran Harga",
                    3,
                    false,
                    false,
                    false,
                    false,
                    "Selalu ada, dikelola via menu Documents (bukan JobType config)",
                    null
                ),
                (
                    "Surat Negosiasi Harga",
                    4,
                    false,
                    false,
                    false,
                    false,
                    "Selalu ada, dikelola via menu Documents (bukan JobType config)",
                    null
                ),
                (
                    "Surat Perintah Mulai Pekerjaan (SPMP)",
                    5,
                    true,
                    true,
                    false,
                    true,
                    "SPMP upload dengan approval Manager",
                    null
                ),
                (
                    "Bill of Quantity (BOQ)",
                    6,
                    true,
                    true,
                    false,
                    true,
                    "BOQ di-generate otomatis oleh sistem",
                    null
                ),
                (
                    "Owner Estimate (OE)",
                    7,
                    true,
                    true,
                    false,
                    true,
                    "Di-generate sistem; approval AM HTE -> Manager",
                    null
                ),
                (
                    "Memorandum",
                    8,
                    true,
                    true,
                    false,
                    true,
                    "Memorandum upload dengan approval Manager",
                    null
                ),
                (
                    "Rencana Kerja dan Syarat-Syarat (RKS)",
                    9,
                    true,
                    true,
                    false,
                    true,
                    "Di-generate sistem; approval AM HTE -> Manager",
                    ProcurementCategory.Jasa
                ),
                (
                    "Risk Assessment (RA)",
                    10,
                    true,
                    true,
                    false,
                    true,
                    "Upload (khusus pengadaan jasa); approval HSE -> AM HTE -> Manager",
                    ProcurementCategory.Jasa
                ),
                (
                    "Market Survey",
                    11,
                    true,
                    false,
                    true,
                    false,
                    "Upload dari HTE; mempengaruhi progress",
                    null
                ),
                (
                    "Justifikasi",
                    12,
                    true,
                    true,
                    false,
                    true,
                    "Justifikasi (>= 300 jt, approval kondisional saat generate flow)",
                    null
                ),
            };

            var wtdSet = context.Set<JobTypeDocuments>();
            var skipAlwaysDocs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Surat Penawaran Harga",
                "Surat Negosiasi Harga",
            };

            foreach (var config in configDocuments)
            {
                if (skipAlwaysDocs.Contains(config.Name))
                    continue;

                var dt = DT(config.Name);
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
                            Sequence = config.Seq,
                            IsMandatory = config.Mandatory,
                            IsGenerated = config.Generated,
                            IsUploadRequired = config.UploadReq,
                            RequiresApproval = config.RequiresApproval,
                            Note = config.Note,
                            ProcurementCategory = config.Category,
                        }
                    );
                }
            }

            await context.SaveChangesAsync();

            async Task<string?> GetRoleIdOrNullAsync(string roleName)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                return role?.Id;
            }

            async Task AddApprovalIfMissing(string docName, string roleName, int level)
            {
                var wtd = await wtdSet
                    .Include(x => x.DocumentType)
                    .Where(x => x.JobTypeId == jobType.JobTypeId && x.DocumentType.Name == docName)
                    .FirstAsync();

                var roleId = await GetRoleIdOrNullAsync(roleName);
                if (roleId == null)
                    return;

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
                    new[] { ("Assistant Manager HTE", 1), ("Manager Transport & Logistic", 2) }
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
                    new[] { ("Assistant Manager HTE", 1), ("Manager Transport & Logistic", 2) }
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
