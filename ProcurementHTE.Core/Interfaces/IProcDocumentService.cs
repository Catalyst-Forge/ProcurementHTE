using Microsoft.AspNetCore.Http;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IProcDocumentService
{
    Task<string?> GetPresignedUrlAsync(string procDocumentId, TimeSpan? expires = null);
    Task<bool> DeleteAsync(string procDocumentId);
    Task<ProcDocuments?> GetByIdAsync(string id);
    Task<IReadOnlyList<ProcDocuments>> ListByProcurementAsync(string procurementId);
    Task<UploadProcDocumentResult> UploadAsync(
        UploadProcDocumentRequest request,
        CancellationToken ct = default
    );
    Task<string> GetPresignedDownloadUrlAsync(
        string procDocumentId,
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
    Task<bool> CanSendApprovalAsync(string procurementId, CancellationToken ct = default);
    Task SendApprovalAsync(string procDocumentId, string requestedByUserId, CancellationToken ct = default);
    Task<string?> GetPresignedQrUrlAsync(string procDocumentId, TimeSpan expiry, CancellationToken ct = default);
    Task<UploadProcDocumentResult> SaveGeneratedAsync(GeneratedProcDocumentRequest request, CancellationToken ct = default);
}
