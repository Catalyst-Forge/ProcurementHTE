using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public class LdpService : ILdpService
    {
        private readonly ILdpRepository _ldpRepository;

        public LdpService(ILdpRepository ldpRepository)
        {
            _ldpRepository =
                ldpRepository ?? throw new ArgumentNullException(nameof(ldpRepository));
        }

        public async Task<(IReadOnlyList<LdpRecapDto> Items, int TotalCount)> GetAllAsync(
            int page,
            int pageSize,
            string? search = null,
            CancellationToken ct = default
        )
        {
            // Validate pagination parameters
            if (page < 1)
                page = 1;
            if (pageSize < 1)
                pageSize = 10;
            if (pageSize > 100)
                pageSize = 100; // Max page size

            return await _ldpRepository.GetAllAsync(page, pageSize, search, ct);
        }

        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            return await _ldpRepository.CountAsync(ct);
        }
    }
}
