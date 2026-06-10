using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository
    {
        public async Task<PagedResult<Procurement>> GetProcurementsForAccrualAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        )
        {
            var allowedStatuses = new[] { "Waiting Pickup", "In Progress", "Completed", "Closed" };
            var query = _context
                .Procurements.Include(p => p.JobType)
                .Include(p => p.Status)
                .Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .Include(p => p.AccrualFilledByUser)
                .AsNoTracking()
                .Where(p =>
                    !string.IsNullOrEmpty(p.SpmpNumber)
                    && p.Status != null
                    && allowedStatuses.Contains(p.Status.StatusName)
                );

            if (!string.IsNullOrEmpty(filter))
            {
                query = filter.ToLower() switch
                {
                    "pending" => query.Where(p => string.IsNullOrEmpty(p.NoAccrual)),
                    "filled" => query.Where(p => !string.IsNullOrEmpty(p.NoAccrual)),
                    _ => query,
                };
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = ApplyAccrualSearch(query, search.Trim());

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.CreatedAt),
                ct: ct
            );
        }

        private static IQueryable<Procurement> ApplyAccrualSearch(
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
