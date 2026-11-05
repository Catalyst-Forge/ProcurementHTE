using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    /// <summary>
    /// Adds the Financial Statement upload to every job type. Contract rules
    /// keep it to LTC and RO work.
    /// </summary>
    public static class FinancialStatementSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            var documentType = await db
                .DocumentTypes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Name == ContractDocumentRules.FinancialStatement);

            if (documentType == null)
            {
                documentType = new DocumentType
                {
                    Name = ContractDocumentRules.FinancialStatement,
                    Description = "Dokumen upload untuk kontrak LTC dan RO",
                };
                db.DocumentTypes.Add(documentType);
                await db.SaveChangesAsync();
            }

            var jobTypeIds = await db.JobTypes.Select(t => t.JobTypeId).ToListAsync();
            var configured = await db
                .JobTypeDocuments.Where(c => c.DocumentTypeId == documentType.DocumentTypeId)
                .Select(c => c.JobTypeId)
                .ToListAsync();

            var missing = jobTypeIds
                .Except(configured)
                .Select(jobTypeId => new JobTypeDocuments
                {
                    JobTypeId = jobTypeId,
                    DocumentTypeId = documentType.DocumentTypeId,
                    Sequence = 99,
                    IsMandatory = true,
                    IsGenerated = false,
                    IsUploadRequired = true,
                    RequiresApproval = false,
                    Note = "Upload sendiri; hanya untuk kontrak LTC dan RO",
                })
                .ToList();

            if (missing.Count > 0)
            {
                db.JobTypeDocuments.AddRange(missing);
                await db.SaveChangesAsync();
            }
        }
    }
}
