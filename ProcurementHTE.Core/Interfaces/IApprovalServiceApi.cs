using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IApprovalServiceApi
    {
        Task<PagedResult<ProcDocumentLiteDto>> GetProcDocumentsByQrTextAsync(
            string qrText,
            int page,
            int pageSize,
            CancellationToken ct = default
        );

        Task<ProcDocumentLiteDto?> UpdateProcDocumentStatusAsync(
            string procDocumentId,
            string newStatus,
            string? reason,
            string? approvedByUserId,
            CancellationToken ct = default
        );

        Task<ProcDocumentLiteDto?> GetProcDocumentByQrCode(
            string qrText,
            CancellationToken ct = default
        );
    }
}
