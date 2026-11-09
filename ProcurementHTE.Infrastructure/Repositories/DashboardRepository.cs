using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;
        private readonly IListSortProvider? _listSort;
        private readonly IListFilterProvider? _listFilter;

        public DashboardRepository(
            AppDbContext context,
            IListSortProvider? listSort = null,
            IListFilterProvider? listFilter = null
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _listSort = listSort;
            _listFilter = listFilter;
        }
    }
}
