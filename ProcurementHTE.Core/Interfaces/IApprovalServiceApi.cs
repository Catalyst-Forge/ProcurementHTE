using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IApprovalServiceApi
    {
        Task<PagedResult<WoDocumentLiteDto>> GetWoDocumentsByQrTextAsync(
            string qrText,
            int page,
            int pageSize,
            CancellationToken ct = default
        );

        Task<WoDocumentLiteDto?> UpdateWoDocumentStatusAsync(
            string woDocumentId,
            string newStatus,
            string? reason,
            string? approvedByUserId,
            CancellationToken ct = default
        );
    }
}
