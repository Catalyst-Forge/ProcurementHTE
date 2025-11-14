using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IProcDocumentRepository
{
    Task<PagedResult<ProcDocumentLiteDto>> GetListByQrTextSameProcurementAsync(
        string qrText,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
    Task<ProcDocumentLiteDto?> UpdateStatusAsync(
        string procDocumentId,
        string newStatus,
        string? reason,
        string? approvedByUserId,
        CancellationToken ct = default
    );
    Task<ProcDocumentLiteDto?> GetProcDocumentByQrCode(
    string QrText,
    CancellationToken ct = default
);
    Task<ProcDocuments?> GetByIdAsync(string id);
    Task<IReadOnlyList<ProcDocuments>> GetByProcurementAsync(string procurementId);
    Task<ProcDocuments?> GetLatestActiveByProcurementAndDocTypeAsync(string woId, string documentTypeId);
    Task AddAsync(ProcDocuments doc);
    Task UpdateAsync(ProcDocuments doc);
    Task DeleteAsync(string id);
    Task SaveAsync();
}
