using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface ILdpService
    {
        Task<(IReadOnlyList<LdpRecapDto> Items, int TotalCount)> GetAllAsync(
            int page,
            int pageSize,
            string? search = null,
            CancellationToken ct = default
        );

        Task<int> CountAsync(CancellationToken ct = default);

        /// <summary>Every row the current search, filters, and sort match, for an export.</summary>
        Task<IReadOnlyList<LdpRecapDto>> GetAllForExportAsync(string? search, CancellationToken ct = default);
    }
}
