using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Utils;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class ProcurementRepository : IProcurementRepository
    {
        private readonly AppDbContext _context;
        private const string PROC_PREFIX = "PROC";
        private const int MAX_RETRY_ATTEMPTS = 5;

        public ProcurementRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        #region Query Methods

        public async Task<Core.Common.PagedResult<Procurement>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct,
            string? userId
        )
        {
            var query = BuildBaseQuery();

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
                query = ApplySearchFilter(query, search.Trim(), fields);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(proc => proc.UserId == userId);
            }

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(w => w.CreatedAt),
                ct: ct
            );
        }

        public async Task<Procurement?> GetByIdAsync(string id)
        {
            return await _context
                .Procurements.Include(procurement => procurement.ProcOffers)
                .Include(procurement => procurement.Status)
                .Include(procurement => procurement.JobType)
                .Include(procurement => procurement.User)
                .Include(procurement => procurement.ProcDetails)
                .Include(procurement => procurement.ProfitLosses)
                .FirstOrDefaultAsync(procurement => procurement.ProcurementId == id);
        }

        public async Task<Procurement?> GetWithSelectedOfferAsync(string id)
        {
            return await _context
                .Procurements.Include(procurement => procurement.VendorOffers)
                .ThenInclude(vendorOffer => vendorOffer.Vendor)
                .FirstOrDefaultAsync(procurement => procurement.ProcurementId == id);
        }

        public async Task<IReadOnlyList<Procurement>> GetRecentByUserAsync(
            string userId,
            int limit,
            CancellationToken ct
        )
        {
            return await _context
                .Procurements.Where(procurement => procurement.UserId == userId)
                .OrderByDescending(procurement => procurement.CreatedAt)
                .Include(procurement => procurement.Status)
                .AsNoTracking()
                .Take(limit)
                .ToListAsync(ct);
        }

        public Task<int> CountAsync(CancellationToken ct)
        {
            return _context.Procurements.CountAsync(ct);
        }

        public async Task<IReadOnlyList<ProcurementStatusCountDto>> GetCountByStatusAsync()
        {
            return await _context
                .Procurements.GroupBy(wo => wo.Status!.StatusName)
                .Select(group => new ProcurementStatusCountDto
                {
                    Status = group.Key,
                    Count = group.Count(),
                })
                .ToListAsync();
        }

        #endregion

        #region Lookup Methods

        public async Task<Status?> GetStatusByNameAsync(string name)
        {
            var normalized = name.Trim();
            return await _context
                .Statuses.AsNoTracking()
                .FirstOrDefaultAsync(status =>
                    status.StatusName != null && EF.Functions.Like(status.StatusName, normalized)
                );
        }

        public Task<List<Status>> GetStatusesAsync()
        {
            return _context
                .Statuses.AsNoTracking()
                .OrderBy(status => status.StatusName)
                .ToListAsync();
        }

        public Task<JobTypes?> GetJobTypeByIdAsync(string id)
        {
            return _context.JobTypes.FirstOrDefaultAsync(job => job.JobTypeId == id);
        }

        public Task<List<JobTypes>> GetJobTypesAsync()
        {
            return _context.JobTypes.OrderBy(job => job.TypeName).ToListAsync();
        }

        #endregion

        #region Command Methods

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

                if (existingOffers.Count != 0)
                    _context.ProcOffers.RemoveRange(existingOffers);

                var validDetails = FilterValidDetails(details);
                var validOffers = FilterValidOffers(offers);

                AssignProcurementIdToDetails(procurement.ProcurementId, validDetails);
                AssignProcurementIdToOffers(procurement.ProcurementId, validOffers);

                if (validDetails.Count != 0)
                    await _context.ProcDetails.AddRangeAsync(validDetails);

                if (validOffers.Count != 0)
                    await _context.ProcOffers.AddRangeAsync(validOffers);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DropProcurementAsync(Procurement procurement)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Procurements.Remove(procurement);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private IQueryable<Procurement> BuildBaseQuery()
        {
            return _context
                .Procurements.Include(procurement => procurement.ProcOffers)
                .Include(procurement => procurement.Status)
                .Include(procurement => procurement.JobType)
                .Include(procurement => procurement.User)
                .Include(procurement => procurement.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .AsSplitQuery()
                .AsNoTracking();
        }

        private static IQueryable<Procurement> ApplySearchFilter(
            IQueryable<Procurement> query,
            string searchTerm,
            ISet<string> fields
        )
        {
            bool byProcNum = fields.Contains("ProcNum", StringComparer.OrdinalIgnoreCase);
            bool byWonum = fields.Contains("Wonum", StringComparer.OrdinalIgnoreCase);
            bool byJobName = fields.Contains("JobName", StringComparer.OrdinalIgnoreCase);
            bool byProjectCode = fields.Contains("ProjectCode", StringComparer.OrdinalIgnoreCase);
            bool bySelectedVendor = fields.Contains(
                "SelectedVendorName",
                StringComparer.OrdinalIgnoreCase
            );
            bool byStatus = fields.Contains("Status", StringComparer.OrdinalIgnoreCase);

            var like = $"%{searchTerm}%";

            return query.Where(procurement =>
                (
                    byProcNum
                    && procurement.ProcNum != null
                    && EF.Functions.Like(procurement.ProcNum, like)
                )
                || (
                    byWonum
                    && procurement.Wonum != null
                    && EF.Functions.Like(procurement.Wonum, like)
                )
                || (
                    byJobName
                    && procurement.JobName != null
                    && EF.Functions.Like(procurement.JobName, like)
                )
                || (
                    byProjectCode
                    && procurement.ProjectCode != null
                    && EF.Functions.Like(procurement.ProjectCode, like)
                )
                || (
                    byStatus
                    && procurement.Status != null
                    && EF.Functions.Like(procurement.Status.StatusName, like)
                )
            );
        }

        private async Task<string> GenerateNextProcNumAsync()
        {
            var lastProcNum = await GetLastProcNumAsync(PROC_PREFIX);
            return SequenceNumberGenerator.NumId(PROC_PREFIX, lastProcNum);
        }

        private async Task<string?> GetLastProcNumAsync(string prefix)
        {
            return await _context
                .Procurements.Where(procurement => procurement.ProcNum!.StartsWith(prefix))
                .OrderByDescending(procurement => procurement.ProcNum)
                .Select(procurement => procurement.ProcNum)
                .FirstOrDefaultAsync();
        }

        private static List<ProcDetail> FilterValidDetails(List<ProcDetail>? details)
        {
            return (details ?? [])
                .Where(detail =>
                    !string.IsNullOrWhiteSpace(detail.ItemName)
                    && detail.Quantity.HasValue
                    && detail.Quantity.Value > 0
                )
                .ToList();
        }

        private static List<ProcOffer> FilterValidOffers(List<ProcOffer>? offers)
        {
            return (offers ?? [])
                .Where(offer => !string.IsNullOrWhiteSpace(offer.ItemPenawaran))
                .ToList();
        }

        private static void AssignProcurementIdToDetails(
            string procurementId,
            List<ProcDetail> details
        )
        {
            foreach (var detail in details)
                detail.ProcurementId = procurementId;
        }

        private static void AssignProcurementIdToOffers(
            string procurementId,
            List<ProcOffer> offers
        )
        {
            foreach (var offer in offers)
                offer.ProcurementId = procurementId;
        }

        private static bool IsUniqueProcNumViolation(DbUpdateException ex) =>
            ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
            && sqlEx.Number == 2627
            && (sqlEx.Message?.Contains("AK_Procurements_ProcNum") ?? false);

        #endregion
    }
}
