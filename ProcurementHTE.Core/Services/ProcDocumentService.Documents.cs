using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public sealed partial class ProcDocumentService
{
    public async Task<string?> GetPresignedUrlAsync(string procDocumentId, TimeSpan? expires = null)
    {
        var doc = await _procDocumentRepository.GetByIdAsync(procDocumentId);
        if (doc is null || string.IsNullOrWhiteSpace(doc.ObjectKey))
            return null;

        return await _objectStorage.GetPresignedUrlAsync(
            _storageOptions.Bucket,
            doc.ObjectKey,
            GetExpiry(expires)
        );
    }

    public async Task<bool> DeleteAsync(string procDocumentId, string deletedByUserId)
    {
        var entity = await _procDocumentRepository.GetByIdAsync(procDocumentId);
        if (entity is null)
            return false;

        if (!string.IsNullOrWhiteSpace(entity.ObjectKey))
            await SafeDeleteAsync(entity.ObjectKey);

        await _procDocumentRepository.DeleteAsync(procDocumentId, deletedByUserId);
        await _procDocumentRepository.SaveAsync();

        return true;
    }

    public async Task<int> DeleteAllByProcurementAsync(
        string procurementId,
        string deletedByUserId
    )
    {
        var documents = await _procDocumentRepository.GetByProcurementAsync(procurementId);
        if (documents == null || documents.Count == 0)
            return 0;

        var deletedCount = 0;
        foreach (var doc in documents)
        {
            if (!string.IsNullOrWhiteSpace(doc.ObjectKey))
                await SafeDeleteAsync(doc.ObjectKey);

            await _procDocumentRepository.DeleteAsync(doc.ProcDocumentId, deletedByUserId);
            deletedCount++;
        }

        await _procDocumentRepository.SaveAsync();
        return deletedCount;
    }

    public Task<ProcDocuments?> GetByIdAsync(string id) => _procDocumentRepository.GetByIdAsync(id);

    public Task<IReadOnlyList<ProcDocuments>> ListByProcurementAsync(string procurementId) =>
        _procDocumentRepository.GetByProcurementAsync(procurementId);

    public async Task<UploadProcDocumentResult> UploadAsync(
        UploadProcDocumentRequest request,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.ProcurementId))
            throw new ArgumentException("ProcurementId wajib diisi", nameof(request.ProcurementId));
        if (string.IsNullOrWhiteSpace(request.DocumentTypeId))
            throw new ArgumentException(
                "DocumentTypeId wajib diisi",
                nameof(request.DocumentTypeId)
            );

        _ = await GetProcurementOrThrowAsync(request.ProcurementId);

        if (request.Content.CanSeek)
            request.Content.Position = 0;

        var objectKey = BuildDocumentObjectKey(
            request.ProcurementId,
            request.DocumentTypeId,
            request.FileName
        );

        await _objectStorage.UploadAsync(
            _storageOptions.Bucket,
            objectKey,
            request.Content,
            request.Size,
            request.ContentType,
            ct
        );

        var entity = new ProcDocuments
        {
            ProcurementId = request.ProcurementId,
            DocumentTypeId = request.DocumentTypeId,
            FileName = request.FileName,
            ObjectKey = objectKey,
            ContentType = request.ContentType,
            Size = request.Size,
            Description = request.Description,
            CreatedAt = request.NowUtc,
            CreatedByUserId = request.UploadedByUserId,
        };

        await _procDocumentRepository.AddAsync(entity);
        await _procDocumentRepository.SaveAsync();

        return new UploadProcDocumentResult
        {
            ProcDocumentId = entity.ProcDocumentId,
            ObjectKey = entity.ObjectKey,
            FileName = entity.FileName,
            Size = entity.Size,
        };
    }
}
