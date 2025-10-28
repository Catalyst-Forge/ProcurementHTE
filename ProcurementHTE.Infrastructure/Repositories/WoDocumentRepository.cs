using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class WoDocumentRepository : IWoDocumentRepository
    {
        private readonly AppDbContext _context;

        public WoDocumentRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<WoDocuments?> GetByIdAsync(string id)
        {
            return await _context.WoDocuments.FirstOrDefaultAsync(d => d.WoDocumentId == id);
        }

        public async Task<IReadOnlyList<WoDocuments>> GetByWorkOrderAsync(string workOrderId)
        {
            return await _context
                .WoDocuments.Where(d => d.WorkOrderId == workOrderId)
                .OrderByDescending(d => d.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<WoDocuments?> GetLatestActiveByWorkOrderAndDocTypeAsync(string woId, string documentTypeId) {
            return await _context.WoDocuments
            .Where(doc => doc.WorkOrderId == woId && doc.DocumentTypeId == documentTypeId && doc.Status != "Deleted")
            .OrderByDescending(doc => doc.CreatedAt)
            .FirstOrDefaultAsync();
        }

        public async Task AddAsync(WoDocuments doc)
        {
            await _context.WoDocuments.AddAsync(doc);
        }

        public async Task UpdateAsync(WoDocuments doc)
        {
            _context.WoDocuments.Update(doc);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await _context.WoDocuments.FirstOrDefaultAsync(d => d.WoDocumentId == id);
            if (entity != null)
            {
                _context.WoDocuments.Remove(entity);
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }























        // Implementasi metode GetListByQrTextSameWoAsync
        public async Task<PagedResult<WoDocumentLiteDto>> GetListByQrTextSameWoAsync(
                    string qrText, int page, int pageSize, CancellationToken ct = default)
        {
            // Parse DocId dari QR → lebih robust
            string? workOrderId = null;
            var parts = qrText.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var docIdStr = parts.FirstOrDefault(p => p.StartsWith("DocId=", StringComparison.OrdinalIgnoreCase))?
                                .Substring("DocId=".Length);

            if (!string.IsNullOrWhiteSpace(docIdStr))
            {
                workOrderId = await _context.WoDocuments.AsNoTracking()
                    .Where(d => d.WoDocumentId == docIdStr)
                    .Select(d => d.WorkOrderId)
                    .FirstOrDefaultAsync(ct);
            }

            // Fallback: berdasarkan QrText penuh + status 'Pending Approval'
            if (string.IsNullOrEmpty(workOrderId))
            {
                workOrderId = await _context.WoDocuments.AsNoTracking()
                    .Where(d => d.QrText == qrText && d.Status == "Pending Approval")
                    .Select(d => d.WorkOrderId)
                    .FirstOrDefaultAsync(ct);
            }

            if (string.IsNullOrEmpty(workOrderId))
                return new PagedResult<WoDocumentLiteDto>(Array.Empty<WoDocumentLiteDto>(), 0);

            var baseQuery = _context.WoDocuments.AsNoTracking()
                .Where(wd => wd.WorkOrderId == workOrderId);

            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(wd => wd.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(wd => new WoDocumentLiteDto(
                    wd.WoDocumentId,
                    wd.WorkOrderId,
                    wd.FileName,
                    wd.Status,
                    wd.QrText!,
                    wd.ObjectKey,
                    wd.Description,
                    wd.CreatedByUserId,
                    wd.CreatedAt
                ))
                .ToListAsync(ct);

            return new PagedResult<WoDocumentLiteDto>(items, total);
        }

        public async Task<WoDocumentLiteDto?> UpdateStatusAsync(
        string woDocumentId, string newStatus, string? reason, string? approvedByUserId, CancellationToken ct = default)
        {
            var entity = await _context.WoDocuments.FirstOrDefaultAsync(d => d.WoDocumentId == woDocumentId, ct);
            if (entity is null) return null;

            if (!DocStatuses.All.Contains(newStatus))
                throw new ArgumentException($"Unknown status '{newStatus}'", nameof(newStatus));

            // (Opsional) rule transisi sederhana
            var from = entity.Status ?? DocStatuses.Uploaded;
            bool allowed = from switch
            {
                var s when s.Equals(DocStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase)
                    => newStatus is DocStatuses.Approved or DocStatuses.Rejected or DocStatuses.Replaced,
                var s when s.Equals(DocStatuses.Uploaded, StringComparison.OrdinalIgnoreCase)
                    => newStatus is DocStatuses.PendingApproval or DocStatuses.Deleted or DocStatuses.Replaced,
                _ => true
            };
            if (!allowed)
                throw new InvalidOperationException($"Transition {from} -> {newStatus} is not allowed.");

            // Update status & kolom approval ringkas
            entity.Status = newStatus;
            if (DocStatuses.IsFinal(newStatus))
            {
                entity.IsApproved = newStatus.Equals(DocStatuses.Approved, StringComparison.OrdinalIgnoreCase);
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
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number is 2601 or 2627))
            {
                // kalau kamu punya unique index (WorkOrderId, DocumentTypeId, Status)
                throw new InvalidOperationException("Status conflict with unique constraint.", ex);
            }

            // map balik ke DTO ringan (sesuaikan dengan DTO-mu)
            return new WoDocumentLiteDto(
                entity.WoDocumentId,
                entity.WorkOrderId,
                entity.FileName,
                entity.Status,
                entity.QrText!,
                entity.ObjectKey,
                entity.Description,
                entity.CreatedByUserId,
                entity.CreatedAt
            );
        }

        public async Task<WoDocumentLiteDto?> GetWoDocumentByQrCode(
            string QrText,
            CancellationToken ct = default)
        {
            var entity = await _context.WoDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.QrText == QrText, ct);
            if (entity is null) return null;
            return new WoDocumentLiteDto(
                entity.WoDocumentId,
                entity.WorkOrderId,
                entity.FileName,
                entity.Status,
                entity.QrText!,
                entity.ObjectKey,
                entity.Description,
                entity.CreatedByUserId,
                entity.CreatedAt
            );
        }
    }
}
