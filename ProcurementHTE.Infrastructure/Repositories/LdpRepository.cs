using ProcurementHTE.Core.Common;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class LdpRepository : ILdpRepository
    {
        private readonly AppDbContext _context;
        private readonly IListSortProvider _listSort;
        private readonly IListFilterProvider _listFilter;

        public LdpRepository(
            AppDbContext context,
            IListSortProvider listSort,
            IListFilterProvider listFilter
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _listSort = listSort;
            _listFilter = listFilter;
        }

        public async Task<(IReadOnlyList<LdpRecapDto> Items, int TotalCount)> GetAllAsync(
            int page,
            int pageSize,
            string? search = null,
            CancellationToken ct = default
        )
        {
            var query = JobsWithLdpData();

            // Apply search filter - focus on No WO and Job Name
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = $"%{search.Trim()}%";
                query = query.Where(p =>
                    (p.Wonum != null && EF.Functions.Like(p.Wonum, searchTerm))
                    || (p.JobName != null && EF.Functions.Like(p.JobName, searchTerm))
                    || (p.SpkNumber != null && EF.Functions.Like(p.SpkNumber, searchTerm))
                    || (p.ProcNum != null && EF.Functions.Like(p.ProcNum, searchTerm))
                );
            }

            // Order by StartDate descending (newest first)
            query = query.OrderByDescending(p => p.StartDate);

            // Get total count
            var totalCount = await query.CountAsync(ct);

            // Sorting and filtering need every row before paging; see LdpRepository.Sort.cs.
            var sort = _listSort.Current;
            var sortProperty = FindColumn(sort?.Key);
            var filters = KnownFilters(_listFilter.Filters);
            var archived = await PresentedArchiveAsync(search, ct);
            var needsAllRows = sortProperty is not null || filters.Count > 0 || archived.Count > 0;
            var procurements = await (
                needsAllRows ? query : query.Skip((page - 1) * pageSize).Take(pageSize)
            ).ToListAsync(ct);

            // Get VendorRoundLetters for selected vendors
            var procurementIds = procurements.Select(p => p.ProcurementId).ToList();
            var vendorRoundLetters = await _context
                .VendorRoundLetters.Where(vrl => procurementIds.Contains(vrl.ProcurementId))
                .AsNoTracking()
                .ToListAsync(ct);

            // Map to DTO
            var items = procurements
                .Select(p => MapToDto(p, vendorRoundLetters))
                .ToList();

            if (needsAllRows)
            {
                var matches = items.Concat(archived).Where(item => filters.All(filter => filter.Matches(item)));
                matches = sortProperty is not null
                    ? Sort(matches, sortProperty, sort!.Descending)
                    : matches.OrderByDescending(item => item.TglMulai);

                items = matches.ToList();
                totalCount = items.Count;
                items = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }

            return (items, totalCount);
        }

        /// <summary>One job as an LDP row, for its detail page. Null when it does not exist.</summary>
        public async Task<LdpRecapDto?> GetByProcurementIdAsync(string procurementId, CancellationToken ct = default)
        {
            var procurement = await JobsWithLdpData().FirstOrDefaultAsync(p => p.ProcurementId == procurementId, ct);
            if (procurement is null)
                return null;

            var letters = await _context
                .VendorRoundLetters.Where(vrl => vrl.ProcurementId == procurementId)
                .AsNoTracking()
                .ToListAsync(ct);
            return MapToDto(procurement, letters);
        }

        // Everything an LDP row is worked out from.
        private IQueryable<Core.Models.Procurement> JobsWithLdpData() =>
            _context
                .Procurements.Include(p => p.JobType)
                .Include(p => p.AppoUser)
                .Include(p => p.PurchaseRequisition)
                .ThenInclude(pr => pr!.StatusHistories)
                .Include(p => p.StatusHistories)
                .Include(p => p.ProcOffers)
                .Include(p => p.ProcDocuments!)
                .ThenInclude(pd => pd.DocumentType)
                .Include(p => p.ProfitLosses)
                .ThenInclude(pl => pl.SelectedVendor)
                .AsNoTracking()
                .AsSplitQuery();

        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            return await _context.Procurements.CountAsync(ct);
        }
    }
}
