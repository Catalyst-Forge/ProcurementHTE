using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class ProcDocumentRepository : IProcDocumentRepository
    {
        private readonly AppDbContext _context;

        public ProcDocumentRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<ProcDocuments?> GetByIdAsync(string id)
        {
            return await _context.ProcDocuments.FirstOrDefaultAsync(d => d.ProcDocumentId == id);
        }

        public async Task<IReadOnlyList<ProcDocuments>> GetByProcurementAsync(string procurementId)
        {
            return await _context
                .ProcDocuments.Where(d => d.ProcurementId == procurementId)
                .OrderByDescending(d => d.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ProcDocuments?> GetLatestActiveByProcurementAndDocTypeAsync(
            string procurementId,
            string documentTypeId
        )
        {
            // Tidak ada soft delete lagi, semua document aktif
            return await _context
                .ProcDocuments.Where(doc =>
                    doc.ProcurementId == procurementId
                    && doc.DocumentTypeId == documentTypeId
                )
                .OrderByDescending(doc => doc.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(ProcDocuments doc)
        {
            await _context.ProcDocuments.AddAsync(doc);
        }

        public async Task UpdateAsync(ProcDocuments doc)
        {
            _context.ProcDocuments.Update(doc);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string id, string deletedByUserId)
        {
            var entity = await _context.ProcDocuments.FirstOrDefaultAsync(d =>
                d.ProcDocumentId == id
            );
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = deletedByUserId;
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        // Implementasi metode GetListByQrTextSameProcurementAsync
        public async Task<PagedResult<ProcDocumentLiteDto>> GetListByQrTextSameProcurementAsync(
            string qrText,
            int page,
            int pageSize,
            CancellationToken ct = default
        )
        {
            // Parse DocId dari QR → lebih robust
            string? procurementId = null;
            var parts = qrText.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var docIdStr = parts
                .FirstOrDefault(p => p.StartsWith("DocId=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("DocId=".Length);

            if (!string.IsNullOrWhiteSpace(docIdStr))
            {
                procurementId = await _context
                    .ProcDocuments.AsNoTracking()
                    .Where(d => d.ProcDocumentId == docIdStr)
                    .Select(d => d.ProcurementId)
                    .FirstOrDefaultAsync(ct);
            }

            // Fallback search by document ID only - QrText dan Status sudah dihapus
            if (string.IsNullOrEmpty(procurementId))
                return new PagedResult<ProcDocumentLiteDto>(Array.Empty<ProcDocumentLiteDto>(), 0);

            var baseQuery = _context
                .ProcDocuments.AsNoTracking()
                .Where(wd => wd.ProcurementId == procurementId);

            var total = await baseQuery.CountAsync(ct);

            var itemsQuery =
                from wd in baseQuery
                join u in _context.Users on wd.CreatedByUserId equals u.Id into gj
                from u in gj.DefaultIfEmpty()
                orderby wd.CreatedAt descending
                select new ProcDocumentLiteDto(
                    wd.ProcDocumentId,
                    wd.ProcurementId,
                    wd.FileName,
                    wd.ObjectKey,
                    wd.Description,
                    wd.CreatedByUserId,
                    u != null ? u.FullName : null,
                    wd.CreatedAt
                );

            var items = await itemsQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

            return new PagedResult<ProcDocumentLiteDto>(items, total);
        }

        /// <summary>
        /// Status sekarang di level PR, method ini deprecated.
        /// Selalu return null - tidak ada update status per document.
        /// </summary>
        public Task<ProcDocumentLiteDto?> UpdateStatusAsync(
            string procDocumentId,
            string newStatus,
            string? reason,
            string? approvedByUserId,
            CancellationToken ct = default
        )
        {
            // No-op: Status tracking moved to PurchaseRequisition level
            return Task.FromResult<ProcDocumentLiteDto?>(null);
        }

        /// <summary>
        /// QrText sekarang di level PR, method ini deprecated.
        /// Selalu return null.
        /// </summary>
        public Task<ProcDocumentLiteDto?> GetProcDocumentByQrCode(
            string QrText,
            CancellationToken ct = default
        )
        {
            // No-op: QR code moved to PurchaseRequisition level
            return Task.FromResult<ProcDocumentLiteDto?>(null);
        }
    }
}
