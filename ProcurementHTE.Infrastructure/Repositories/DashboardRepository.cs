using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));
    }
}
