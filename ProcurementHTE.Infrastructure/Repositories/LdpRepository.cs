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

        public LdpRepository(AppDbContext context, IListSortProvider listSort)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _listSort = listSort;
        }

        public async Task<(IReadOnlyList<LdpRecapDto> Items, int TotalCount)> GetAllAsync(
            int page,
            int pageSize,
            string? search = null,
            CancellationToken ct = default
        )
        {
            // Base query - start from Procurements
            var query = _context
                .Procurements.Include(p => p.JobType)
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

            // A sorted report needs every row before it can page; see LdpRepository.Sort.cs.
            var sort = _listSort.Current;
            var sortProperty = FindSortProperty(sort);
            var procurements = await (
                sortProperty is null ? query.Skip((page - 1) * pageSize).Take(pageSize) : query
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

            if (sortProperty is not null)
            {
                items = Sort(items, sortProperty, sort!.Descending)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }

            return (items, totalCount);
        }

        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            return await _context.Procurements.CountAsync(ct);
        }
    }
}
