using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository
    {
        public async Task<PagedResult<Procurement>> GetProcurementsForArPickupAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        )
        {
            var allowedStatuses = new[] { "Waiting Pickup", "In Progress", "Completed", "Closed" };
            var query = BuildArPickupQuery()
                .Where(p => p.Status != null && allowedStatuses.Contains(p.Status.StatusName));

            if (!string.IsNullOrEmpty(filter))
            {
                query = filter.ToLower() switch
                {
                    "pending" => query.Where(p => string.IsNullOrEmpty(p.ArUserId)),
                    "picked" => query.Where(p => !string.IsNullOrEmpty(p.ArUserId)),
                    _ => query,
                };
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = ApplyArPickupSearch(query, search.Trim());

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.CreatedAt),
                ct: ct
            );
        }

        public async Task<PagedResult<Procurement>> GetMyArPickupsAsync(
            string arUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        )
        {
            var query = BuildArPickupQuery().Where(p => p.ArUserId == arUserId);

            if (!string.IsNullOrWhiteSpace(search))
                query = ApplyArPickupSearch(query, search.Trim());

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.CreatedAt),
                ct: ct
            );
        }

        private IQueryable<Procurement> BuildArPickupQuery()
        {
            return _context
                .Procurements.Include(p => p.JobType)
                .Include(p => p.Status)
                .Include(p => p.AppoUser)
                .Include(p => p.ArUser)
                .Include(p => p.AccrualFilledByUser)
                .Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .AsNoTracking();
        }

        private static IQueryable<Procurement> ApplyArPickupSearch(
            IQueryable<Procurement> query,
            string search
        )
        {
            var searchTerm = $"%{search}%";
            return query.Where(p =>
                (p.Wonum != null && EF.Functions.Like(p.Wonum, searchTerm))
                || (p.JobName != null && EF.Functions.Like(p.JobName, searchTerm))
                || (p.SpkNumber != null && EF.Functions.Like(p.SpkNumber, searchTerm))
                || (p.ProcNum != null && EF.Functions.Like(p.ProcNum, searchTerm))
                || (p.NoAccrual != null && EF.Functions.Like(p.NoAccrual, searchTerm))
            );
        }
    }
}
