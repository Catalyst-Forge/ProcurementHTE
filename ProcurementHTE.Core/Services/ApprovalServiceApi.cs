using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public class ApprovalServiceApi : IApprovalServiceApi
    {
        private readonly IWoDocumentRepository _repo;

        public ApprovalServiceApi(IWoDocumentRepository repo) => _repo = repo;

        public Task<PagedResult<WoDocumentLiteDto>> GetWoDocumentsByQrTextAsync(
            string qrText,
            int page,
            int pageSize,
            CancellationToken ct = default
        ) => _repo.GetListByQrTextSameWoAsync(qrText, page, pageSize, ct);

        public Task<WoDocumentLiteDto?> UpdateWoDocumentStatusAsync(
            string woDocumentId,
            string newStatus,
            string? reason,
            string? approvedByUserId,
            CancellationToken ct = default
        ) => _repo.UpdateStatusAsync(woDocumentId, newStatus, reason, approvedByUserId, ct);

        public async Task<WoDocumentLiteDto?> GetWoDocumentByQrCode(
            string qrText,
            CancellationToken ct = default
        )
        {
            return await _repo.GetWoDocumentByQrCode(qrText, ct);
        }
    }
}
