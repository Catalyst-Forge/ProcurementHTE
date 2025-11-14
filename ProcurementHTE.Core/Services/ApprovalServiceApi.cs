using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public class ApprovalServiceApi : IApprovalServiceApi
    {
        private readonly IProcDocumentRepository _repo;

        public ApprovalServiceApi(IProcDocumentRepository repo) => _repo = repo;

        public Task<PagedResult<ProcDocumentLiteDto>> GetProcDocumentsByQrTextAsync(
            string qrText,
            int page,
            int pageSize,
            CancellationToken ct = default
        ) => _repo.GetListByQrTextSameProcurementAsync(qrText, page, pageSize, ct);

        public Task<ProcDocumentLiteDto?> UpdateProcDocumentStatusAsync(
            string procDocumentId,
            string newStatus,
            string? reason,
            string? approvedByUserId,
            CancellationToken ct = default
        ) => _repo.UpdateStatusAsync(procDocumentId, newStatus, reason, approvedByUserId, ct);

        public async Task<ProcDocumentLiteDto?> GetProcDocumentByQrCode(
            string qrText,
            CancellationToken ct = default
        )
        {
            return await _repo.GetProcDocumentByQrCode(qrText, ct);
        }
    }
}
