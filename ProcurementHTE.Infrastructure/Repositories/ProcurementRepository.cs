using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class ProcurementRepository : IProcurementRepository
    {
        private readonly AppDbContext _context;
        private const string PROC_PREFIX = "PROC";
        private const int MAX_RETRY_ATTEMPTS = 5;

        public ProcurementRepository(AppDbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));
    }
}
