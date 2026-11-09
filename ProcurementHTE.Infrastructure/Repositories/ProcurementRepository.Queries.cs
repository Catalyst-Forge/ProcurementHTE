using ProcurementHTE.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository
    {
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
                query = query.Where(proc => proc.UserId == userId);

            if (_listFilter != null)
                query = ApplyColumnFilters(query, _listFilter.Filters);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => SortProcurements(q, o => o.OrderByDescending(p => p.CreatedAt)),
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
                .ThenInclude(pl => pl.SelectedVendor)
                .Include(procurement => procurement.AppoUser)
                .Include(procurement => procurement.ApInvoiceUser)
                .Include(procurement => procurement.ArUser)
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

        public Task<int> CountAsync(CancellationToken ct) => _context.Procurements.CountAsync(ct);

        public async Task<IReadOnlyList<ProcurementStatusCountDto>> GetCountByStatusAsync()
        {
            return await _context
                .Procurements.GroupBy(procurement => procurement.Status!.StatusName)
                .Select(group => new ProcurementStatusCountDto
                {
                    Status = group.Key,
                    Count = group.Count(),
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Procurement>> GetAllForSelectionAsync()
        {
            return await _context
                .Procurements.Include(procurement => procurement.JobType)
                .Include(procurement => procurement.Status)
                .Include(procurement => procurement.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .AsSplitQuery()
                .AsNoTracking()
                .OrderByDescending(procurement => procurement.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Procurement>> GetPublishedForFinanceAsync(
            CancellationToken ct = default
        ) => await PublishedForFinance(_context).ToListAsync(ct);

        /// <summary>The finance tables' query, public so a test can print its SQL Server form.</summary>
        public static IQueryable<Procurement> PublishedForFinance(AppDbContext context)
        {
            var published = Core.Finance.FinanceTable.PublishedStatuses.ToList();
            return context
                .Procurements.Include(p => p.JobType)
                .Include(p => p.Status)
                .Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .Where(p => p.Status != null && published.Contains(p.Status.StatusName))
                .OrderByDescending(p => p.CreatedAt)
                .AsSplitQuery()
                .AsNoTracking();
        }

        public async Task<Core.Common.PagedResult<Procurement>> GetProcurementsForAppoApprovalAsync(
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            var query = BuildBaseQuery()
                .Where(p => p.Status != null && p.Status.StatusName == "Waiting Pickup");

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
                query = ApplySearchFilter(query, search.Trim(), fields);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => SortProcurements(q, o => o.OrderByDescending(p => p.CreatedAt)),
                ct: ct
            );
        }

        public async Task<Core.Common.PagedResult<Procurement>> GetMyAppoPickupsAsync(
            string appoUserId,
            int page,
            int pageSize,
            string? search,
            ISet<string> fields,
            CancellationToken ct
        )
        {
            var query = BuildBaseQuery()
                .Include(p => p.AppoUser)
                .Where(p => p.AppoUserId == appoUserId);

            if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
                query = ApplySearchFilter(query, search.Trim(), fields);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => SortProcurements(q, o => o.OrderByDescending(p => p.CreatedAt)),
                ct: ct
            );
        }
    }
}
