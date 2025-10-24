using Microsoft.AspNetCore.Http;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IWoDocumentService
{
    Task<string?> GetPresignedUrlAsync(string woDocumentId, TimeSpan? expires = null);
    Task<bool> DeleteAsync(string woDocumentId);
    Task<WoDocuments?> GetByIdAsync(string id);
    Task<IReadOnlyList<WoDocuments>> ListByWorkOrderAsync(string workOrderId);
    Task<UploadWoDocumentResult> UploadAsync(UploadWoDocumentRequest request, CancellationToken ct = default);
    Task<string> GetPresignedDownloadUrlAsync(string woDocumentId, TimeSpan ttl, CancellationToken ct = default);
    Task<string> GetPresignedPreviewUrlAsync(string id, TimeSpan expiry, CancellationToken ct = default);
}
