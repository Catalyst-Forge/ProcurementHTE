using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class PurchaseRequisitionTrackingRepository : IPurchaseRequisitionTrackingRepository
    {
        private readonly AppDbContext _context;

        public PurchaseRequisitionTrackingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PurchaseRequisition?> GetWithTrackingIncludesByPrNumberAsync(
            string prNumber,
            CancellationToken ct = default
        )
        {
            return await _context
                .PurchaseRequisitions.AsNoTracking()
                .Include(p => p.CreatedByUser)
                .Include(p => p.IspaSubmittedByUser)
                .Include(p => p.PoSubmittedByUser)
                .Include(p => p.RejectedByUser)
                .Include(p => p.StatusHistories)
                .ThenInclude(h => h.ChangedByUser)
                .Include(p => p.Procurements)
                .ThenInclude(proc => proc.ProcDocuments)
                .Include(p => p.Procurements)
                .ThenInclude(proc => proc.JobType)
                .ThenInclude(jt => jt!.JobTypeDocuments)
                .ThenInclude(jtd => jtd.DocumentType)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.PrNumber == prNumber, ct);
        }

        public async Task<PurchaseRequisition?> GetWithTrackingIncludesByPrIdAsync(
            string prId,
            CancellationToken ct = default
        )
        {
            return await _context
                .PurchaseRequisitions.AsNoTracking()
                .Include(p => p.CreatedByUser)
                .Include(p => p.IspaSubmittedByUser)
                .Include(p => p.PoSubmittedByUser)
                .Include(p => p.RejectedByUser)
                .Include(p => p.StatusHistories)
                .ThenInclude(h => h.ChangedByUser)
                .Include(p => p.Procurements)
                .ThenInclude(proc => proc.ProcDocuments)
                .Include(p => p.Procurements)
                .ThenInclude(proc => proc.JobType)
                .ThenInclude(jt => jt!.JobTypeDocuments)
                .ThenInclude(jtd => jtd.DocumentType)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.PrId == prId, ct);
        }

        public async Task<PurchaseRequisition?> GetByIdAsync(
            string prId,
            CancellationToken ct = default
        )
        {
            return await _context.PurchaseRequisitions.FindAsync([prId], ct);
        }

        public async Task<PurchaseRequisition?> GetByIdWithProcurementsAsync(
            string prId,
            CancellationToken ct = default
        )
        {
            return await _context
                .PurchaseRequisitions.Include(p => p.Procurements)
                .FirstOrDefaultAsync(p => p.PrId == prId, ct);
        }

        public async Task<User?> GetUserByIdAsync(string userId, CancellationToken ct = default)
        {
            return await _context.Users.FindAsync([userId], ct);
        }

        public Task UpdateAsync(PurchaseRequisition pr, CancellationToken ct = default)
        {
            _context.Entry(pr).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public async Task AddStatusHistoryAsync(
            string prId,
            PurchaseRequisitionStatus status,
            string? changedByUserId,
            string? note,
            CancellationToken ct = default
        )
        {
            _context.PurchaseRequisitionStatusHistories.Add(
                new PurchaseRequisitionStatusHistory
                {
                    PrId = prId,
                    Status = status,
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = changedByUserId,
                    Note = note,
                }
            );
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, bool>> GetNeedsJustifikasiMapAsync(
            List<string> procurementIds,
            CancellationToken ct = default
        )
        {
            var result = new Dictionary<string, bool>();

            if (procurementIds.Count == 0)
                return result;

            // Get latest ProfitLoss for each procurement and check if value > 300 million
            var profitLossData = await _context
                .ProfitLosses.AsNoTracking()
                .Where(p => procurementIds.Contains(p.ProcurementId))
                .GroupBy(p => p.ProcurementId)
                .Select(g => new
                {
                    ProcurementId = g.Key,
                    LatestPnl = g.OrderByDescending(p => p.CreatedAt).FirstOrDefault(),
                })
                .ToListAsync(ct);

            foreach (var procId in procurementIds)
            {
                var pnlData = profitLossData.FirstOrDefault(p => p.ProcurementId == procId);
                var needsJustifikasi = false;

                if (pnlData?.LatestPnl != null)
                {
                    var bestFinalOffer = pnlData.LatestPnl.SelectedVendorFinalOffer;

                    if (bestFinalOffer <= 0m)
                    {
                        // Calculate from vendor offers
                        var offers = await _context
                            .VendorOffers.AsNoTracking()
                            .Where(o => o.ProfitLossId == pnlData.LatestPnl.ProfitLossId)
                            .ToListAsync(ct);

                        if (offers.Count > 0)
                        {
                            bestFinalOffer = offers
                                .GroupBy(o => o.VendorId)
                                .Select(group =>
                                {
                                    var perItem = group
                                        .GroupBy(x => x.ProcOfferId)
                                        .Select(gg =>
                                        {
                                            var last = gg.OrderBy(x => x.Round).Last();
                                            return last.Price
                                                * last.QuantityItem
                                                * last.QuantityOfUnit;
                                        });
                                    return perItem.Sum();
                                })
                                .DefaultIfEmpty(0m)
                                .Min();
                        }
                    }

                    needsJustifikasi = bestFinalOffer > 300_000_000m;
                }

                result[procId] = needsJustifikasi;
            }

            return result;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
