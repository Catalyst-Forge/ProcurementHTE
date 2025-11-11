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
    Task<UploadWoDocumentResult> UploadAsync(
        UploadWoDocumentRequest request,
        CancellationToken ct = default
    );
    Task<string> GetPresignedDownloadUrlAsync(
        string woDocumentId,
        TimeSpan ttl,
        CancellationToken ct = default
    );
    Task<string> GetPresignedViewUrlByObjectKeyAsync(
        string objectKey,
        string? fileName,
        string? contentType,
        TimeSpan ttl,
        CancellationToken ct = default
    );

    Task<string> GetPresignedPreviewUrlAsync(
        string id,
        TimeSpan expiry,
        CancellationToken ct = default
    );
    Task<bool> CanSendApprovalAsync(string workOrderId, CancellationToken ct = default);
    Task SendApprovalAsync(string workOrderId, string requestedByUserId, CancellationToken ct = default);
    Task<string?> GetPresignedQrUrlAsync(string woDocumentId, TimeSpan expiry, CancellationToken ct = default);
    Task<UploadWoDocumentResult> SaveGeneratedAsync(GeneratedWoDocumentRequest request, CancellationToken ct = default);
}
