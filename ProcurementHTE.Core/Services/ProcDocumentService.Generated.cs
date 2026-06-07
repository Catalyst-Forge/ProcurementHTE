#nullable enable

using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public sealed partial class ProcDocumentService
{
    public async Task<UploadProcDocumentResult> SaveGeneratedAsync(
        GeneratedProcDocumentRequest request,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Bytes is null || request.Bytes.Length == 0)
            throw new ArgumentException("File hasil generate kosong.", nameof(request.Bytes));

        _ = await GetProcurementOrThrowAsync(request.ProcurementId);
        var (existingDoc, previousObjectKey) = await ResolveExistingDocumentAsync(request);
        var objectKey = BuildGeneratedObjectKey(
            request.ProcurementId,
            request.DocumentTypeId,
            request.FileName
        );

        using var ms = new MemoryStream(request.Bytes, writable: false);
        await _objectStorage.UploadAsync(
            _storageOptions.Bucket,
            objectKey,
            ms,
            ms.Length,
            request.ContentType,
            ct
        );

        var entity = existingDoc is null
            ? await AddGeneratedDocumentAsync(request, objectKey)
            : await UpdateGeneratedDocumentAsync(existingDoc, request, objectKey);

        await _procDocumentRepository.SaveAsync();

        if (
            !string.IsNullOrWhiteSpace(previousObjectKey)
            && !string.Equals(previousObjectKey, entity.ObjectKey, StringComparison.Ordinal)
        )
        {
            await SafeDeleteAsync(previousObjectKey);
        }

        return new UploadProcDocumentResult
        {
            ProcDocumentId = entity.ProcDocumentId,
            ObjectKey = entity.ObjectKey,
            FileName = entity.FileName,
            Size = entity.Size,
        };
    }

    private async Task<(ProcDocuments? ExistingDoc, string? PreviousObjectKey)>
        ResolveExistingDocumentAsync(GeneratedProcDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProcDocumentId))
            return (null, null);

        var existingDoc = await _procDocumentRepository.GetByIdAsync(request.ProcDocumentId);
        if (existingDoc is null)
        {
            throw new KeyNotFoundException(
                $"ProcDocument dengan ID '{request.ProcDocumentId}' tidak ditemukan."
            );
        }

        if (!string.Equals(existingDoc.ProcurementId, request.ProcurementId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "ProcDocument tidak sesuai dengan procurement yang dimaksud."
            );

        return (existingDoc, existingDoc.ObjectKey);
    }

    private async Task<ProcDocuments> AddGeneratedDocumentAsync(
        GeneratedProcDocumentRequest request,
        string objectKey
    )
    {
        var entity = ApplyGeneratedDocumentValues(new ProcDocuments(), request, objectKey);
        await _procDocumentRepository.AddAsync(entity);
        return entity;
    }

    private async Task<ProcDocuments> UpdateGeneratedDocumentAsync(
        ProcDocuments entity,
        GeneratedProcDocumentRequest request,
        string objectKey
    )
    {
        ApplyGeneratedDocumentValues(entity, request, objectKey);
        await _procDocumentRepository.UpdateAsync(entity);
        return entity;
    }

    private static ProcDocuments ApplyGeneratedDocumentValues(
        ProcDocuments entity,
        GeneratedProcDocumentRequest request,
        string objectKey
    )
    {
        entity.ProcurementId = request.ProcurementId;
        entity.DocumentTypeId = request.DocumentTypeId;
        entity.FileName = request.FileName;
        entity.ObjectKey = objectKey;
        entity.ContentType = request.ContentType;
        entity.Size = request.Bytes!.Length;
        entity.Description = request.Description;
        entity.CreatedAt = request.CreatedAt == default ? DateTime.UtcNow : request.CreatedAt;
        entity.CreatedByUserId = request.GeneratedByUserId;
        return entity;
    }
}
