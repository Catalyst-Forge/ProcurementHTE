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
                orderBy: q => q.OrderByDescending(w => w.CreatedAt),
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
                orderBy: q => q.OrderByDescending(w => w.CreatedAt),
                ct: ct
            );
        }
    }
}
