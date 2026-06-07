using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository
    {
        public async Task StoreProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        )
        {
            var validDetails = FilterValidDetails(details);
            var validOffers = FilterValidOffers(offers);

            for (var attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                using var transactionDb = await _context.Database.BeginTransactionAsync();

                try
                {
                    procurement.ProcNum = await GenerateNextProcNumAsync();
                    AssignProcurementIdToDetails(procurement.ProcurementId, validDetails);
                    AssignProcurementIdToOffers(procurement.ProcurementId, validOffers);

                    await _context.Procurements.AddAsync(procurement);

                    if (validDetails.Count != 0)
                        await _context.ProcDetails.AddRangeAsync(validDetails);

                    if (validOffers.Count != 0)
                        await _context.ProcOffers.AddRangeAsync(validOffers);

                    await _context.SaveChangesAsync();
                    await transactionDb.CommitAsync();
                    return;
                }
                catch (DbUpdateException ex) when (IsUniqueProcNumViolation(ex))
                {
                    await transactionDb.RollbackAsync();
                    if (attempt == MAX_RETRY_ATTEMPTS)
                    {
                        throw new InvalidOperationException(
                            "Gagal membuat nomor Procurement unik setelah beberapa percobaan.",
                            ex
                        );
                    }
                }
                catch
                {
                    await transactionDb.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task UpdateProcurementAsync(Procurement procurement)
        {
            try
            {
                _context.Entry(procurement).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context
                    .Procurements.AsNoTracking()
                    .AnyAsync(procurement =>
                        procurement.ProcurementId == procurement.ProcurementId
                    );

                if (!exists)
                    throw new KeyNotFoundException(
                        $"Procurement dengan ID {procurement.ProcurementId} tidak ditemukan"
                    );

                throw new InvalidOperationException(
                    "Data telah diubah oleh user lain. Silakan refresh dan coba lagi"
                );
            }
        }

        public async Task UpdateProcurementWithDetailsAsync(
            Procurement procurement,
            List<ProcDetail> details,
            List<ProcOffer> offers
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Entry(procurement).State = EntityState.Modified;

                var existingDetails = await _context
                    .ProcDetails.Where(detail => detail.ProcurementId == procurement.ProcurementId)
                    .ToListAsync();

                var existingOffers = await _context
                    .ProcOffers.Where(offer => offer.ProcurementId == procurement.ProcurementId)
                    .ToListAsync();

                if (existingDetails.Count != 0)
                    _context.ProcDetails.RemoveRange(existingDetails);

                var validDetails = FilterValidDetails(details);
                AssignProcurementIdToDetails(procurement.ProcurementId, validDetails);

                if (validDetails.Count != 0)
                    await _context.ProcDetails.AddRangeAsync(validDetails);

                var validOffers = FilterValidOffers(offers);
                AssignProcurementIdToOffers(procurement.ProcurementId, validOffers);
                await UpdateOffersAsync(existingOffers, validOffers);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(Procurement procurement, string deletedByUserId)
        {
            var entityToDelete = await _context.Procurements.FirstOrDefaultAsync(p =>
                p.ProcurementId == procurement.ProcurementId
            );

            if (entityToDelete == null)
                return;

            entityToDelete.IsDeleted = true;
            entityToDelete.DeletedAt = DateTime.UtcNow;
            entityToDelete.DeletedBy = deletedByUserId;

            if (!string.IsNullOrEmpty(entityToDelete.ProcNum))
                entityToDelete.ProcNum = $"-{entityToDelete.ProcNum}";

            await _context.SaveChangesAsync();
        }

        private async Task UpdateOffersAsync(
            IReadOnlyList<ProcOffer> existingOffers,
            IReadOnlyList<ProcOffer> validOffers
        )
        {
            for (var i = 0; i < validOffers.Count; i++)
            {
                if (i < existingOffers.Count)
                {
                    UpdateExistingOffer(existingOffers[i], validOffers[i]);
                }
                else
                {
                    await _context.ProcOffers.AddAsync(validOffers[i]);
                }
            }

            if (existingOffers.Count > validOffers.Count)
                _context.ProcOffers.RemoveRange(existingOffers.Skip(validOffers.Count));
        }

        private void UpdateExistingOffer(ProcOffer existing, ProcOffer updated)
        {
            existing.ItemPenawaran = updated.ItemPenawaran;
            existing.Qty = updated.Qty;
            existing.Unit = updated.Unit;
            existing.UnitRevenue = updated.UnitRevenue;
            _context.Entry(existing).State = EntityState.Modified;
        }
    }
}
