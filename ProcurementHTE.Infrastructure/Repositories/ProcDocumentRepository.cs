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
            return await _context
                .ProcDocuments.Where(doc =>
                    doc.ProcurementId == procurementId
                    && doc.DocumentTypeId == documentTypeId
                    && doc.Status != "Deleted"
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

        public async Task DeleteAsync(string id)
        {
            var entity = await _context.ProcDocuments.FirstOrDefaultAsync(d => d.ProcDocumentId == id);
            if (entity != null)
            {
                _context.ProcDocuments.Remove(entity);
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

            // Fallback: berdasarkan QrText penuh + status 'Pending Approval'
            if (string.IsNullOrEmpty(procurementId))
            {
                procurementId = await _context
                    .ProcDocuments.AsNoTracking()
                    .Where(d => d.QrText == qrText && d.Status == "Pending Approval")
                    .Select(d => d.ProcurementId)
                    .FirstOrDefaultAsync(ct);
            }

            if (string.IsNullOrEmpty(procurementId))
                return new PagedResult<ProcDocumentLiteDto>(Array.Empty<ProcDocumentLiteDto>(), 0);

            var baseQuery = _context
                .ProcDocuments.AsNoTracking()
                .Where(wd => wd.ProcurementId == procurementId);

            var total = await baseQuery.CountAsync(ct);

            var itemsQuery = from wd in baseQuery
                             join u in _context.Users on wd.CreatedByUserId equals u.Id into gj
                             from u in gj.DefaultIfEmpty()
                             orderby wd.CreatedAt descending
                             select new ProcDocumentLiteDto(
                                 wd.ProcDocumentId,
                                 wd.ProcurementId,
                                 wd.FileName,
                                 wd.Status,
                                 wd.QrText!,
                                 wd.ObjectKey,
                                 wd.Description,
                                 wd.CreatedByUserId,
                                 u != null ? u.FullName : null,
                                 wd.CreatedAt
                             );

            var items = await itemsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<ProcDocumentLiteDto>(items, total);
        }

        public async Task<ProcDocumentLiteDto?> UpdateStatusAsync(
            string procDocumentId,
            string newStatus,
            string? reason,
            string? approvedByUserId,
            CancellationToken ct = default
        )
        {
            var entity = await _context.ProcDocuments.FirstOrDefaultAsync(
                d => d.ProcDocumentId == procDocumentId,
                ct
            );
            if (entity is null)
                return null;

            if (!DocStatuses.All.Contains(newStatus))
                throw new ArgumentException($"Unknown status '{newStatus}'", nameof(newStatus));

            // (Opsional) rule transisi sederhana
            var from = entity.Status ?? DocStatuses.Uploaded;
            bool allowed = from switch
            {
                var s
                    when s.Equals(
                        DocStatuses.PendingApproval,
                        StringComparison.OrdinalIgnoreCase
                    ) => newStatus
                    is DocStatuses.Approved
                        or DocStatuses.Rejected
                        or DocStatuses.Replaced,
                var s when s.Equals(DocStatuses.Uploaded, StringComparison.OrdinalIgnoreCase) =>
                    newStatus
                        is DocStatuses.PendingApproval
                            or DocStatuses.Deleted
                            or DocStatuses.Replaced,
                _ => true,
            };
            if (!allowed)
                throw new InvalidOperationException(
                    $"Transition {from} -> {newStatus} is not allowed."
                );

            // Update status & kolom approval ringkas
            entity.Status = newStatus;
            if (DocStatuses.IsFinal(newStatus))
            {
                entity.IsApproved = newStatus.Equals(
                    DocStatuses.Approved,
                    StringComparison.OrdinalIgnoreCase
                );
                entity.ApprovedAt = DateTime.UtcNow;
                entity.ApprovedByUserId = approvedByUserId;
            }
            else
            {
                entity.IsApproved = null;
                entity.ApprovedAt = null;
                entity.ApprovedByUserId = null;
            }

            if (!string.IsNullOrWhiteSpace(reason))
                entity.Description = reason;

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sqlEx && (sqlEx.Number is 2601 or 2627))
            {
                // kalau kamu punya unique index (ProcurementId, DocumentTypeId, Status)
                throw new InvalidOperationException("Status conflict with unique constraint.", ex);
            }

            // map balik ke DTO ringan (sesuaikan dengan DTO-mu)
            var createdByName = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == entity.CreatedByUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);

            return new ProcDocumentLiteDto(
                entity.ProcDocumentId,
                entity.ProcurementId,
                entity.FileName,
                entity.Status,
                entity.QrText!,
                entity.ObjectKey,
                entity.Description,
                entity.CreatedByUserId,
                createdByName,
                entity.CreatedAt
            );
        }

        public async Task<ProcDocumentLiteDto?> GetProcDocumentByQrCode(
            string QrText,
            CancellationToken ct = default)
        {
            var entity = await _context.ProcDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.QrText == QrText, ct);
            if (entity is null) return null;
            var createdByName = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == entity.CreatedByUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);

            return new ProcDocumentLiteDto(
                entity.ProcDocumentId,
                entity.ProcurementId,
                entity.FileName,
                entity.Status,
                entity.QrText!,
                entity.ObjectKey,
                entity.Description,
                entity.CreatedByUserId,
                createdByName,
                entity.CreatedAt
            );
        }
    }
}
