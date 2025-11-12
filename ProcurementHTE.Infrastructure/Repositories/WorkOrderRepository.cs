using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Utils;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class WorkOrderRepository : IWorkOrderRepository
    {
        private readonly AppDbContext _context;
        private const string WO_PREFIX = "WO";
        private const int MAX_RETRY_ATTEMPTS = 5;

        public WorkOrderRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        #region Query Methods

        public async Task<Core.Common.PagedResult<WorkOrder>> GetAllAsync(int page, int pageSize, string? search, ISet<string> fields, CancellationToken ct)
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

        public async Task<WorkOrder?> GetByIdAsync(string id)
        {
            return await _context
                .WorkOrders.Include(wo => wo.WoOffers)
                .Include(wo => wo.Status)
                .Include(wo => wo.WoType)
                .Include(wo => wo.User)
                .Include(wo => wo.WoDetails)
                .FirstOrDefaultAsync(t => t.WorkOrderId == id);
        }

        public async Task<WorkOrder?> GetWithSelectedOfferAsync(string id)
        {
            return await _context
                .WorkOrders.Include(wo => wo.VendorOffers)
                .ThenInclude(vo => vo.Vendor)
                .FirstOrDefaultAsync(wo => wo.WorkOrderId == id);
        }

        public async Task<IReadOnlyList<WorkOrder>> GetRecentByUserAsync(string userId, int limit, CancellationToken ct)
        {
            return await _context
                .WorkOrders.Where(wo => wo.UserId == userId)
                .OrderByDescending(wo => wo.CreatedAt)
                .Include(wo => wo.Status)
                .AsNoTracking()
                .Take(limit)
                .ToListAsync(ct);
        }

        public Task<int> CountAsync(CancellationToken ct)
        {
            return _context.WorkOrders.CountAsync(ct);
        }

        public async Task<IReadOnlyList<WoStatusCountDto>> GetCountByStatusAsync()
        {
            return await _context
                .WorkOrders.GroupBy(wo => wo.Status!.StatusName)
                .Select(group => new WoStatusCountDto { Status = group.Key, Count = group.Count() })
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

        public Task<WoTypes?> GetWoTypeByIdAsync(string id)
        {
            return _context.WoTypes.FirstOrDefaultAsync(wt => wt.WoTypeId == id);
        }

        public Task<List<WoTypes>> GetWoTypesAsync()
        {
            return _context.WoTypes.OrderBy(wt => wt.TypeName).ToListAsync();
        }

        #endregion

        #region Command Methods

        public async Task StoreWorkOrderWithDetailsAsync(
            WorkOrder wo,
            List<WoDetail> details,
            List<WoOffer> offers
        )
        {
            var validDetails = FilterValidDetails(details);
            var validOffers = FilterValidOffers(offers);

            for (var attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                using var transactionDb = await _context.Database.BeginTransactionAsync();

                try
                {
                    wo.WoNum = await GenerateNextWoNumAsync();

                    AssignWorkOrderIdToDetails(wo.WorkOrderId, validDetails);
                    AssignWorkOrderIdToOffers(wo.WorkOrderId, validOffers);

                    await _context.WorkOrders.AddAsync(wo);

                    if (validDetails.Count != 0)
                        await _context.WoDetails.AddRangeAsync(validDetails);

                    if (validOffers.Count != 0)
                        await _context.WoOffers.AddRangeAsync(validOffers);

                    await _context.SaveChangesAsync();
                    await transactionDb.CommitAsync();
                    return;
                }
                catch (DbUpdateException ex) when (IsUniqueWoNumViolation(ex))
                {
                    await transactionDb.RollbackAsync();
                    if (attempt == MAX_RETRY_ATTEMPTS)
                    {
                        throw new InvalidOperationException(
                            "Gagal membuat nomor Work Order unik setelah beberapa percobaan.",
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

        public async Task UpdateWorkOrderAsync(WorkOrder wo)
        {
            try
            {
                _context.Entry(wo).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.WorkOrders.AsNoTracking().AnyAsync(wo =>
                    wo.WorkOrderId == wo.WorkOrderId
                );

                if (!exists)
                    throw new KeyNotFoundException(
                        $"Work Order dengan ID {wo.WorkOrderId} tidak ditemukan"
                    );

                throw new InvalidOperationException(
                    "Data telah diubah oleh user lain. Silakan refresh dan coba lagi"
                );
            }
        }

        public async Task UpdateWorkOrderWithDetailsAsync(
            WorkOrder wo,
            List<WoDetail> details,
            List<WoOffer> offers
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Entry(wo).State = EntityState.Modified;

                var existingDetails = await _context
                    .WoDetails.Where(d => d.WorkOrderId == wo.WorkOrderId)
                    .ToListAsync();

                var existingOffers = await _context
                    .WoOffers.Where(o => o.WorkOrderId == wo.WorkOrderId)
                    .ToListAsync();

                if (existingDetails.Count != 0)
                    _context.WoDetails.RemoveRange(existingDetails);

                if (existingOffers.Count != 0)
                    _context.WoOffers.RemoveRange(existingOffers);

                var validDetails = FilterValidDetails(details);
                var validOffers = FilterValidOffers(offers);

                AssignWorkOrderIdToDetails(wo.WorkOrderId, validDetails);
                AssignWorkOrderIdToOffers(wo.WorkOrderId, validOffers);

                if (validDetails.Count != 0)
                    await _context.WoDetails.AddRangeAsync(validDetails);

                if (validOffers.Count != 0)
                    await _context.WoOffers.AddRangeAsync(validOffers);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DropWorkOrderAsync(WorkOrder wo)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.WorkOrders.Remove(wo);
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

        private IQueryable<WorkOrder> BuildBaseQuery()
        {
            return _context
                .WorkOrders.Include(wo => wo.WoOffers)
                .Include(wo => wo.Status)
                .Include(wo => wo.WoType)
                .Include(wo => wo.User)
                .Include(wo => wo.WoDetails)
                .AsNoTracking();
        }

        private static IQueryable<WorkOrder> ApplySearchFilter(
            IQueryable<WorkOrder> query,
            string searchTerm,
            ISet<string> fields
        )
        {
            //var predicates = new List<Func<WorkOrder, bool>>();

            //if (fields.Contains("WoNum"))
            //    predicates.Add(wo => wo.WoNum != null && wo.WoNum.Contains(searchTerm));

            //if (fields.Contains("Description"))
            //    predicates.Add(wo => wo.Description != null && wo.Description.Contains(searchTerm));

            //if (fields.Contains("WoNumLetter"))
            //    predicates.Add(wo => wo.WoNumLetter != null && wo.WoNumLetter.Contains(searchTerm));

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

            bool byWoNum = fields.Contains("WoNum", StringComparer.OrdinalIgnoreCase);
            bool byDescription = fields.Contains("Description", StringComparer.OrdinalIgnoreCase);
            bool byWoNumLetter = fields.Contains("WoNumLetter", StringComparer.OrdinalIgnoreCase);
            bool byWbs = fields.Contains("WBS", StringComparer.OrdinalIgnoreCase);
            bool byGlAccount = fields.Contains("GlAccount", StringComparer.OrdinalIgnoreCase);
            bool byStatus = fields.Contains("Status", StringComparer.OrdinalIgnoreCase);

            var like = $"%{searchTerm}%";

            return query.Where(wo =>
                (byWoNum && wo.WoNum != null && EF.Functions.Like(wo.WoNum, like)) ||
                (byDescription && wo.Description != null && EF.Functions.Like(wo.Description, like)) ||
                (byWoNumLetter && wo.WoNumLetter != null && EF.Functions.Like(wo.WoNumLetter, like)) ||
                (byWbs && wo.WBS != null && EF.Functions.Like(wo.WBS, like)) ||
                (byGlAccount && wo.GlAccount != null && EF.Functions.Like(wo.GlAccount, like)) ||
                (byStatus && wo.Status != null && EF.Functions.Like(wo.Status.StatusName, like))
            );
        }

        private async Task<string> GenerateNextWoNumAsync()
        {
            var lastWoNum = await GetLastWoNumAsync(WO_PREFIX);
            return SequenceNumberGenerator.NumId(WO_PREFIX, lastWoNum);
        }

        private async Task<string?> GetLastWoNumAsync(string prefix)
        {
            return await _context
                .WorkOrders.Where(w => w.WoNum!.StartsWith(prefix))
                .OrderByDescending(w => w.WoNum)
                .Select(w => w.WoNum)
                .FirstOrDefaultAsync();
        }

        private static List<WoDetail> FilterValidDetails(List<WoDetail>? details)
        {
            return (details ?? [])
                .Where(detail =>
                    !string.IsNullOrWhiteSpace(detail.ItemName)
                    && !string.IsNullOrWhiteSpace(detail.Unit)
                    && detail.Quantity > 0
                )
                .ToList();
        }

        private static List<WoOffer> FilterValidOffers(List<WoOffer>? offers)
        {
            return (offers ?? [])
                .Where(offer => !string.IsNullOrWhiteSpace(offer.ItemPenawaran))
                .ToList();
        }

        private static void AssignWorkOrderIdToDetails(string workOrderId, List<WoDetail> details)
        {
            foreach (var detail in details)
                detail.WorkOrderId = workOrderId;
        }

        private static void AssignWorkOrderIdToOffers(string workOrderId, List<WoOffer> offers)
        {
            foreach (var offer in offers)
                offer.WorkOrderId = workOrderId;
        }

        private static bool IsUniqueWoNumViolation(DbUpdateException ex) =>
            ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
            && sqlEx.Number == 2627
            && (sqlEx.Message?.Contains("AK_WorkOrders_WoNum") ?? false);

        #endregion
    }
}
