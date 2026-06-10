using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository
    {
        public async Task<PagedResult<Procurement>> GetProcurementsForApInvoiceAsync(
            int page,
            int pageSize,
            string? search,
            string? filter,
            CancellationToken ct
        )
        {
            var query = BuildApInvoiceQuery()
                .Where(p =>
                    !string.IsNullOrEmpty(p.AppoUserId)
                    && p.ProcurementStatus == ProcurementStatus.DonePO
                );

            if (!string.IsNullOrEmpty(filter))
            {
                query = filter.ToLower() switch
                {
                    "pending" => query.Where(p => string.IsNullOrEmpty(p.ApInvoiceUserId)),
                    "filled" => query.Where(p => !string.IsNullOrEmpty(p.ApInvoiceUserId)),
                    _ => query,
                };
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = ApplyApInvoiceSearch(query, search.Trim());

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.CreatedAt),
                ct: ct
            );
        }

        public async Task<PagedResult<Procurement>> GetMyApInvoicePickupsAsync(
            string apInvoiceUserId,
            int page,
            int pageSize,
            string? search,
            CancellationToken ct
        )
        {
            var query = BuildApInvoiceQuery().Where(p => p.ApInvoiceUserId == apInvoiceUserId);

            if (!string.IsNullOrWhiteSpace(search))
                query = ApplyApInvoiceSearch(query, search.Trim());

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.ApInvoicePickedUpAt ?? p.CreatedAt),
                ct: ct
            );
        }

        private IQueryable<Procurement> BuildApInvoiceQuery()
        {
            return _context
                .Procurements.Include(p => p.JobType)
                .Include(p => p.Status)
                .Include(p => p.AppoUser)
                .Include(p => p.ApInvoiceUser)
                .Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .AsNoTracking();
        }

        private static IQueryable<Procurement> ApplyApInvoiceSearch(
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
                || (p.SANo != null && EF.Functions.Like(p.SANo, searchTerm))
                || (p.SP3No != null && EF.Functions.Like(p.SP3No, searchTerm))
            );
        }
    }
}
