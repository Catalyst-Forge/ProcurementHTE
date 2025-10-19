using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public class WorkOrderRepository : IWorkOrderRepository
    {
        private readonly AppDbContext _context;

        public WorkOrderRepository(AppDbContext context)
        {
            _context = context;
        }

        private async Task<string?> GetLastWoNumAsync(string prefix)
        {
            return await _context
                .WorkOrders.Where(w => w.WoNum!.StartsWith(prefix))
                .OrderByDescending(w => w.WoNum)
                .Select(w => w.WoNum)
                .FirstOrDefaultAsync();
        }

        private static bool IsUniqueWoNumViolation(DbUpdateException ex) =>
            ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
            && sqlEx.Number == 2627
            && (sqlEx.Message?.Contains("AK_WorkOrders_WoNum") ?? false);

        public async Task<PagedResult<WorkOrder>> GetAllAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            var query = _context
                .WorkOrders.Include(wo => wo.Status)
                .Include(wo => wo.WoType)
                .Include(wo => wo.User)
                .Include(wo => wo.Vendor)
                .Include(wo => wo.WoDetails)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
            {
                var s = search.Trim();
                bool byWoNum = fields.Contains("WoNum");
                bool byDesc = fields.Contains("Description");
                bool byLetter = fields.Contains("WoLetter");
                bool byWbs = fields.Contains("WBS");
                bool byGlAccount = fields.Contains("GlAccount");
                bool byStat = fields.Contains("Status");

                query = query.Where(w =>
                    (byWoNum && w.WoNum != null && w.WoNum.Contains(s))
                    || (byDesc && w.Description != null && w.Description.Contains(s))
                    || (byLetter && w.WoLetter != null && w.WoLetter.Contains(s))
                    || (byWbs && w.WBS != null && w.WBS.Contains(s))
                    || (byGlAccount && w.GlAccount != null && w.GlAccount.Contains(s))
                    || (byStat && w.Status != null && w.Status.StatusName.Contains(s))
                );
            }

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(w => w.CreatedAt),
                ct: ct
            );
        }

        public async Task<WorkOrder?> GetByIdAsync(string id)
        {
            var wo = await _context
                .WorkOrders.Include(wo => wo.Status)
                .Include(wo => wo.WoType)
                .Include(wo => wo.User)
                .Include(wo => wo.Vendor)
                .FirstOrDefaultAsync(t => t.WorkOrderId == id);

            if (wo == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(wo.WorkOrderId))
            {
                var details = await _context
                    .WoDetails.Where(d => d.WorkOrderId == wo.WorkOrderId)
                    .AsNoTracking()
                    .ToListAsync();

                wo.WoDetails = details;
            }

            return wo;
        }

        public async Task<IReadOnlyList<WorkOrder>> GetRecentByUserAsync(
            string userId,
            int limit,
            CancellationToken ct
        )
        {
            return await _context
                .WorkOrders.Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .Include(w => w.Status)
                .AsNoTracking()
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<Status?> GetStatusByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var normalized = name.Trim().ToLower();
            return await _context
                .Statuses.AsNoTracking()
                .FirstOrDefaultAsync(s => s.StatusName.ToLower() == normalized);
        }

        public async Task<List<WoTypes>> GetWoTypesAsync()
        {
            return await _context.WoTypes.OrderBy(wt => wt.TypeName).ToListAsync();
        }

        public async Task<WoTypes?> GetWoTypeByIdAsync(string id)
        {
            return await _context.WoTypes.FirstOrDefaultAsync(x => x.WoTypeId == id);
        }

        public async Task<List<Status>> GetStatusesAsync()
        {
            return await _context.Statuses.OrderBy(s => s.StatusName).ToListAsync();
        }

        public async Task<WorkOrder?> GetWithOffersAsync(string id)
        {
            return await _context.WorkOrders.FirstOrDefaultAsync(x => x.WorkOrderId == id);
        }

        public async Task<WorkOrder?> GetWithSelectedOfferAsync(string id)
        {
            return await _context
                .WorkOrders.Include(x => x.VendorOffers)
                .ThenInclude(x => x!.Vendor)
                .FirstOrDefaultAsync(x => x.WorkOrderId == id);
        }

        public async Task<int> CountAsync(CancellationToken ct)
        {
            return await _context.WorkOrders.CountAsync(ct);
        }

        public async Task StoreWorkOrderAsync(WorkOrder wo)
        {
            await _context.AddAsync(wo);
            await _context.SaveChangesAsync();
        }

        public async Task StoreWorkOrderWithDetailsAsync(WorkOrder wo, List<WoDetail> details)
        {
            const string prefix = "WO";
            const int maxRetry = 5;

            details = (details ?? new())
                .Where(d =>
                    !string.IsNullOrWhiteSpace(d.ItemName)
                    && !string.IsNullOrWhiteSpace(d.Unit)
                    && d.Quantity > 0
                )
                .ToList();

            for (var attempt = 1; attempt <= maxRetry; attempt++)
            {
                using var transactionDb = await _context.Database.BeginTransactionAsync();

                try
                {
                    var lastId = await GetLastWoNumAsync(prefix);
                    wo.WoNum = SequenceNumberGenerator.NumId(prefix, lastId);

                    foreach (var detail in details)
                    {
                        detail.WorkOrderId = wo.WorkOrderId;
                    }

                    await _context.WorkOrders.AddAsync(wo);

                    if (details != null && details.Count > 0)
                    {
                        await _context.WoDetails.AddRangeAsync(details);
                    }

                    await _context.SaveChangesAsync();
                    await transactionDb.CommitAsync();
                    return;
                }
                catch (DbUpdateException ex) when (IsUniqueWoNumViolation(ex))
                {
                    await transactionDb.RollbackAsync();
                    if (attempt == maxRetry)
                    {
                        throw;
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
                //_context.WorkOrders.Update(wo);
                _context.Entry(wo).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exists = await _context.WorkOrders.AnyAsync(w =>
                    w.WorkOrderId == wo.WorkOrderId
                );
                if (!exists)
                    throw new KeyNotFoundException(
                        $"Work Order dengan ID {wo.WorkOrderId} tidak ditemukan"
                    );

                throw new InvalidOperationException(
                    "Data telah diubah oleh user lain. Silakan refresh dan coba lagi",
                    ex
                );
            }
        }

        public async Task DropWorkOrderAsync(WorkOrder wo)
        {
            _context.WorkOrders.Remove(wo);
            await _context.SaveChangesAsync();
        }
    }
}
