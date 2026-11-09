using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository : IProcurementRepository
    {
        private readonly AppDbContext _context;
        private readonly IListSortProvider _listSort;
        private readonly IListFilterProvider? _listFilter;
        private const string PROC_PREFIX = "PROC";
        private const int MAX_RETRY_ATTEMPTS = 5;

        public ProcurementRepository(
            AppDbContext context,
            IListSortProvider listSort,
            IListFilterProvider? listFilter = null
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _listSort = listSort;
            _listFilter = listFilter;
        }

        /// <summary>
        /// The column menu's value search on the Procurements list (f.spmp, f.jobName, ...).
        /// Public so a test can print its SQL Server form.
        /// </summary>
        public static IQueryable<Procurement> ApplyColumnFilters(
            IQueryable<Procurement> query,
            IReadOnlyDictionary<string, string> filters
        )
        {
            foreach (var (key, raw) in filters)
            {
                var value = raw.Trim();
                if (value.Length == 0)
                    continue;

                var like = $"%{value}%";
                switch (key.ToLowerInvariant())
                {
                    case "spmp":
                        query = query.Where(p => p.SpmpNumber != null && EF.Functions.Like(p.SpmpNumber, like));
                        break;
                    case "jobname":
                        query = query.Where(p => p.JobName != null && EF.Functions.Like(p.JobName, like));
                        break;
                    case "jobtype":
                        query = query.Where(p => p.JobType != null && EF.Functions.Like(p.JobType.TypeName, like));
                        break;
                    case "region":
                        var regions = Enum.GetValues<Core.Enums.ProjectRegion>()
                            .Where(r => r.ToString().Contains(value, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        query = query.Where(p => regions.Contains(p.ProjectRegion));
                        break;
                    case "vendor":
                        query = query.Where(p =>
                            p.ProfitLosses.Any(pl => EF.Functions.Like(pl.SelectedVendor.VendorName, like))
                        );
                        break;
                    default:
                        // Start Date and the other sortable columns: the shared search by column type.
                        if (ListSortColumns.Procurements.TryGetValue(key, out var column))
                            query = query.Where(ListFilterExtensions.Matches(column, value));
                        break;
                }
            }

            return query;
        }

        private IOrderedQueryable<Procurement> SortProcurements(
            IQueryable<Procurement> query,
            Func<IQueryable<Procurement>, IOrderedQueryable<Procurement>> defaultOrder
        ) =>
            query.SortBy(
                _listSort.Current,
                ListSortColumns.Procurements,
                p => p.ProcurementId,
                defaultOrder
            );
    }
}
