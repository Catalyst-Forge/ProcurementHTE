using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    public static partial class JobTypeAngkutanSeeder
    {
        public static async Task SeedAsync(AppDbContext context, RoleManager<Role> roleManager)
        {
            using var tx = await context.Database.BeginTransactionAsync();

            var typeName = "Angkutan";
            var LegacyTypeName = "Angkutan";
            var description = "Konfigurasi dokumen untuk pengadaan Angkutan";
            var jobType = await context.JobTypes.FirstOrDefaultAsync(t => t.TypeName == typeName);

            if (jobType == null)
            {
                await context.JobTypes.FirstOrDefaultAsync(t => t.TypeName == LegacyTypeName);
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

            var wtdSet = context.Set<JobTypeDocuments>();
            var skipAlwaysDocs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Surat Penawaran Harga",
                "Surat Negosiasi Harga",
            };

            foreach (var config in ConfigDocuments)
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
