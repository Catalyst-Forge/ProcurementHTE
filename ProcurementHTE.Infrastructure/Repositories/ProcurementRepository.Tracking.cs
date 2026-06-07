using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository
    {
        public async Task<Procurement?> GetWithTrackingDataAsync(
            string procurementId,
            CancellationToken ct = default
        )
        {
            return await BuildTrackingQuery()
                .FirstOrDefaultAsync(p => p.ProcurementId == procurementId, ct);
        }

        public async Task<Procurement?> GetByProcNumWithTrackingAsync(
            string procNum,
            CancellationToken ct = default
        )
        {
            return await BuildTrackingQuery()
                .FirstOrDefaultAsync(
                    p => p.ProcNum == procNum || p.Wonum == procNum || p.SpmpNumber == procNum,
                    ct
                );
        }

        public async Task<IReadOnlyList<Procurement>> GetByPrIdWithTrackingAsync(
            string prId,
            CancellationToken ct = default
        )
        {
            return await _context
                .Procurements.Where(p => p.PrId == prId)
                .Include(p => p.JobType)
                .ThenInclude(jt => jt!.JobTypeDocuments)
                .ThenInclude(jtd => jtd.DocumentType)
                .Include(p => p.Status)
                .Include(p => p.IspaSubmittedByUser)
                .Include(p => p.PoSubmittedByUser)
                .Include(p => p.HardcopySubmittedByUser)
                .Include(p => p.RejectedByUser)
                .Include(p => p.ApprovalSentByUser)
                .Include(p => p.StatusHistories.OrderBy(h => h.ChangedAt))
                .ThenInclude(h => h.ChangedByUser)
                .Include(p => p.ProcDocuments!)
                .ThenInclude(pd => pd.DocumentType)
                .AsNoTracking()
                .AsSplitQuery()
                .OrderBy(p => p.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<bool> UpdateStatusWithHistoryAsync(
            string procurementId,
            ProcurementStatus newStatus,
            string? changedByUserId = null,
            string? note = null,
            CancellationToken ct = default
        )
        {
            var procurement = await _context.Procurements.FirstOrDefaultAsync(
                p => p.ProcurementId == procurementId,
                ct
            );

            if (procurement == null)
                return false;

            var oldStatus = procurement.ProcurementStatus;
            procurement.ProcurementStatus = newStatus;
            procurement.UpdatedAt = DateTime.UtcNow;

            var historyEntry = new ProcurementStatusHistory
            {
                Id = Guid.NewGuid().ToString(),
                ProcurementId = procurementId,
                Status = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = changedByUserId,
                Note = note ?? $"Status changed from {oldStatus} to {newStatus}",
            };

            _context.ProcurementStatusHistories.Add(historyEntry);

            try
            {
                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<
            IReadOnlyList<ProcurementStatusCountDto>
        > GetCountByProcurementStatusAsync(CancellationToken ct = default)
        {
            return await _context
                .Procurements.GroupBy(p => p.ProcurementStatus)
                .Select(group => new ProcurementStatusCountDto
                {
                    Status = group.Key.ToString(),
                    Count = group.Count(),
                })
                .ToListAsync(ct);
        }

        private IQueryable<Procurement> BuildTrackingQuery()
        {
            return _context
                .Procurements.Include(p => p.JobType)
                .ThenInclude(jt => jt!.JobTypeDocuments)
                .ThenInclude(jtd => jtd.DocumentType)
                .Include(p => p.Status)
                .Include(p => p.PurchaseRequisition)
                .Include(p => p.IspaSubmittedByUser)
                .Include(p => p.PoSubmittedByUser)
                .Include(p => p.HardcopySubmittedByUser)
                .Include(p => p.RejectedByUser)
                .Include(p => p.ApprovalSentByUser)
                .Include(p => p.AnalystHteUser)
                .Include(p => p.AssistantManagerUser)
                .Include(p => p.ManagerUser)
                .Include(p => p.StatusHistories.OrderBy(h => h.ChangedAt))
                .ThenInclude(h => h.ChangedByUser)
                .Include(p => p.ProcDocuments!)
                .ThenInclude(pd => pd.DocumentType)
                .Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.Items)
                .AsNoTracking()
                .AsSplitQuery();
        }
    }
}
