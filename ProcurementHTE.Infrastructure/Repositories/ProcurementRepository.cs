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
        private const string PROC_PREFIX = "PROC";
        private const int MAX_RETRY_ATTEMPTS = 5;

        public ProcurementRepository(AppDbContext context, IListSortProvider listSort)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _listSort = listSort;
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
