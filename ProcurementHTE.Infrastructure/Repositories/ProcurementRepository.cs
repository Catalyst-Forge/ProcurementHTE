using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        public async Task<Core.Common.PagedResult<Procurement>> GetAllAsync(int page, int pageSize, string? search, ISet<string> fields, CancellationToken ct)
        {
            var query = BuildBaseQuery();

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
                query = ApplySearchFilter(query, search.Trim(), fields);

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
                .Procurements
                .Include(p => p.ProcOffers)
                .Include(p => p.Status)
                .Include(p => p.JobTypeConfig)
                .Include(p => p.User)
                .Include(p => p.ProcDetails)
                .FirstOrDefaultAsync(t => t.ProcurementId == id);
        }

        public async Task<Procurement?> GetWithSelectedOfferAsync(string id)
        {
            return await _context
                .Procurements.Include(wo => wo.VendorOffers)
                .ThenInclude(vo => vo.Vendor)
                .FirstOrDefaultAsync(wo => wo.ProcurementId == id);
        }

        public async Task<IReadOnlyList<Procurement>> GetRecentByUserAsync(string userId, int limit, CancellationToken ct)
        {
            return await _context
                .Procurements.Where(wo => wo.UserId == userId)
                .OrderByDescending(wo => wo.CreatedAt)
                .Include(wo => wo.Status)
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
                .Select(group => new ProcurementStatusCountDto { Status = group.Key, Count = group.Count() })
                .ToListAsync();
        }

        #endregion

        #region Lookup Methods

        public async Task<Status?> GetStatusByNameAsync(string name)
        {
            var normalized = name.Trim();
            return await _context
                .Statuses.AsNoTracking()
                .FirstOrDefaultAsync(s => s.StatusName != null && EF.Functions.Like(s.StatusName, normalized));
        }

        public Task<List<Status>> GetStatusesAsync()
        {
            return _context.Statuses.AsNoTracking().OrderBy(s => s.StatusName).ToListAsync();
        }

        public Task<JobTypes?> GetJobTypeByIdAsync(string id)
        {
            return _context.JobTypes.FirstOrDefaultAsync(wt => wt.JobTypeId == id);
        }

        public Task<List<JobTypes>> GetJobTypesAsync()
        {
            return _context.JobTypes.OrderBy(wt => wt.TypeName).ToListAsync();
        }

        #endregion

        #region Command Methods

        public async Task StoreProcurementWithDetailsAsync(
            Procurement wo,
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
                    wo.ProcNum = await GenerateNextProcNumAsync();

                    AssignProcurementIdToDetails(wo.ProcurementId, validDetails);
                    AssignProcurementIdToOffers(wo.ProcurementId, validOffers);

                    await _context.Procurements.AddAsync(wo);

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

        public async Task UpdateProcurementAsync(Procurement wo)
        {
            try
            {
                _context.Entry(wo).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Procurements.AsNoTracking().AnyAsync(wo =>
                    wo.ProcurementId == wo.ProcurementId
                );

                if (!exists)
                    throw new KeyNotFoundException(
                        $"Procurement dengan ID {wo.ProcurementId} tidak ditemukan"
                    );

                throw new InvalidOperationException(
                    "Data telah diubah oleh user lain. Silakan refresh dan coba lagi"
                );
            }
        }

        public async Task UpdateProcurementWithDetailsAsync(
            Procurement wo,
            List<ProcDetail> details,
            List<ProcOffer> offers
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Entry(wo).State = EntityState.Modified;

                var existingDetails = await _context
                    .ProcDetails.Where(d => d.ProcurementId == wo.ProcurementId)
                    .ToListAsync();

                var existingOffers = await _context
                    .ProcOffers.Where(o => o.ProcurementId == wo.ProcurementId)
                    .ToListAsync();

                if (existingDetails.Count != 0)
                    _context.ProcDetails.RemoveRange(existingDetails);

                if (existingOffers.Count != 0)
                    _context.ProcOffers.RemoveRange(existingOffers);

                var validDetails = FilterValidDetails(details);
                var validOffers = FilterValidOffers(offers);

                AssignProcurementIdToDetails(wo.ProcurementId, validDetails);
                AssignProcurementIdToOffers(wo.ProcurementId, validOffers);

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

        public async Task DropProcurementAsync(Procurement wo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Procurements.Remove(wo);
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
                .Procurements
                .Include(p => p.ProcOffers)
                .Include(p => p.Status)
                .Include(p => p.JobTypeConfig)
                .Include(p => p.User)
                .Include(p => p.ProcDetails)
                .AsNoTracking();
        }

        private static IQueryable<Procurement> ApplySearchFilter(
            IQueryable<Procurement> query,
            string searchTerm,
            ISet<string> fields
        )
        {
            //var predicates = new List<Func<Procurement, bool>>();

            //if (fields.Contains("ProcNum"))
            //    predicates.Add(wo => wo.ProcNum != null && wo.ProcNum.Contains(searchTerm));

            //if (fields.Contains("Description"))
            //    predicates.Add(wo => wo.Description != null && wo.Description.Contains(searchTerm));

            //if (fields.Contains("SpkNumber"))
            //    predicates.Add(wo => wo.SpkNumber != null && wo.SpkNumber.Contains(searchTerm));

            //if (fields.Contains("WBS"))
            //    predicates.Add(wo => wo.WBS != null && wo.WBS.Contains(searchTerm));

            //if (fields.Contains("GlAccount"))
            //    predicates.Add(wo => wo.GlAccount != null && wo.GlAccount.Contains(searchTerm));

            //if (fields.Contains("Status"))
            //    predicates.Add(wo =>
            //        wo.Status != null && wo.Status.StatusName.Contains(searchTerm)
            //    );

            //if (predicates.Count == 0)
            //    return query;

            //return query.Where(wo => predicates.Any(p => p(wo)));

            bool byProcNum = fields.Contains("ProcNum", StringComparer.OrdinalIgnoreCase);
            bool byJobName = fields.Contains("JobName", StringComparer.OrdinalIgnoreCase);
            bool byJobType = fields.Contains("JobType", StringComparer.OrdinalIgnoreCase);
            bool byProjectCode = fields.Contains("ProjectCode", StringComparer.OrdinalIgnoreCase);
            bool bySelectedVendor = fields.Contains("SelectedVendorName", StringComparer.OrdinalIgnoreCase);
            bool byStatus = fields.Contains("Status", StringComparer.OrdinalIgnoreCase);

            var like = $"%{searchTerm}%";

            return query.Where(p =>
                (byProcNum && p.ProcNum != null && EF.Functions.Like(p.ProcNum, like)) ||
                (byJobName && p.JobName != null && EF.Functions.Like(p.JobName, like)) ||
                (byJobType && p.JobType != null && EF.Functions.Like(p.JobType, like)) ||
                (byProjectCode && p.ProjectCode != null && EF.Functions.Like(p.ProjectCode, like)) ||
                (bySelectedVendor && p.SelectedVendorName != null && EF.Functions.Like(p.SelectedVendorName, like)) ||
                (byStatus && p.Status != null && EF.Functions.Like(p.Status.StatusName, like))
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
                .Procurements.Where(w => w.ProcNum!.StartsWith(prefix))
                .OrderByDescending(w => w.ProcNum)
                .Select(w => w.ProcNum)
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

        private static void AssignProcurementIdToDetails(string procurementId, List<ProcDetail> details)
        {
            foreach (var detail in details)
                detail.ProcurementId = procurementId;
        }

        private static void AssignProcurementIdToOffers(string procurementId, List<ProcOffer> offers)
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
