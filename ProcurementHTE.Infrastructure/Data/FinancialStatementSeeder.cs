using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Data
{
    /// <summary>
    /// Adds the Financial Statement upload to every job type. Contract rules
    /// keep it to LTC work.
    /// </summary>
    public static class FinancialStatementSeeder
    {
        private const string Note = "Upload sendiri, tidak wajib; hanya untuk kontrak LTC";

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
                    Description = Note,
                };
                db.DocumentTypes.Add(documentType);
                await db.SaveChangesAsync();
            }

            var jobTypeIds = await db.JobTypes.Select(t => t.JobTypeId).ToListAsync();
            var existing = await db
                .JobTypeDocuments.Where(c => c.DocumentTypeId == documentType.DocumentTypeId)
                .ToListAsync();
            existing.ForEach(c => c.Note = Note);
            documentType.Description = Note;
            var configured = existing.Select(c => c.JobTypeId);

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
                    Note = Note,
                })
                .ToList();

            db.JobTypeDocuments.AddRange(missing);
            await db.SaveChangesAsync();
        }
    }
}
