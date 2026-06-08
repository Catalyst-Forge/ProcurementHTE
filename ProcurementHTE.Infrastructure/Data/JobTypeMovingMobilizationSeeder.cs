using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static partial class JobTypeMovingMobilizationSeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            // === 0) TRANSAKSI (opsional tapi aman)
            using var tx = await context.Database.BeginTransactionAsync();

            // === 1) Pastikan JobType ada (lookup by TypeName)
            var typeName = "Moving";
            var legacyTypeName = "Moving & Mobilization";
            var description = "Konfigurasi dokumen untuk pengadaan Moving & Mobilization";

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

            // === 2) Pastikan DocumentTypes yang diperlukan ada (lookup by Name)
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
            var wtdSet = context.Set<JobTypeDocuments>();

            var skipAlwaysDocs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Surat Penawaran Harga",
                "Surat Negosiasi Harga",
            };

            foreach (var c in ConfigDocuments)
            {
                if (skipAlwaysDocs.Contains(c.Name))
                    continue; // SPH/SNH tidak dikonfigurasi via JobTypeDocuments

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

            foreach (var (doc, steps) in ApprovalMatrix)
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
