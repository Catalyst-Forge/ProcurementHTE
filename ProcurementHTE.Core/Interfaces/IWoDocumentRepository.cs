using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IWoDocumentRepository
{
    Task<PagedResult<WoDocumentLiteDto>> GetListByQrTextSameWoAsync(
        string qrText,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
    Task<WoDocumentLiteDto?> UpdateStatusAsync(
        string woDocumentId,
        string newStatus,
        string? reason,
        string? approvedByUserId,
        CancellationToken ct = default
    );
    Task<WoDocuments?> GetByIdAsync(string id);
    Task<IReadOnlyList<WoDocuments>> GetByWorkOrderAsync(string workOrderId);
    Task AddAsync(WoDocuments doc);
    Task UpdateAsync(WoDocuments doc);
    Task DeleteAsync(string id);
    Task SaveAsync();
}
