using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface ILdpRepository
    {
        Task<(IReadOnlyList<LdpRecapDto> Items, int TotalCount)> GetAllAsync(
            int page,
            int pageSize,
            string? search = null,
            CancellationToken ct = default
        );

        Task<int> CountAsync(CancellationToken ct = default);

        Task<LdpRecapDto?> GetByProcurementIdAsync(string procurementId, CancellationToken ct = default);

        /// <summary>One Arsip LDP row as an LDP row, for its detail page. Null when it does not exist.</summary>
        Task<LdpRecapDto?> GetArchiveByIdAsync(string archiveRowId, CancellationToken ct = default);
    }
}
