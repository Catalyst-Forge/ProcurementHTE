namespace ProcurementHTE.Core.Services;

public sealed partial class ProcDocumentService
{
    public async Task<string> GetPresignedDownloadUrlAsync(
        string procDocumentId,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        var doc = await _procDocumentRepository.GetByIdAsync(procDocumentId);
        if (doc is null || string.IsNullOrWhiteSpace(doc.ObjectKey))
            throw new KeyNotFoundException("Dokumen tidak ditemukan.");

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["response-content-disposition"] =
                $"attachment; filename=\"{Uri.EscapeDataString(doc.FileName)}\"",
            ["response-content-type"] = doc.ContentType,
        };

        return await _objectStorage.GetPresignedUrlHeaderAsync(
            _storageOptions.Bucket,
            doc.ObjectKey,
            ttl,
            headers,
            ct
        );
    }

    public async Task<string> GetPresignedViewUrlByObjectKeyAsync(
        string objectKey,
        string? fileName,
        string? contentType,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("ObjectKey tidak boleh kosong", nameof(objectKey));

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            headers["response-content-disposition"] =
                $"inline; filename=\"{Uri.EscapeDataString(fileName!)}\"";
        }

        if (!string.IsNullOrWhiteSpace(contentType))
            headers["response-content-type"] = contentType!;

        if (headers.Count == 0)
        {
            return await _objectStorage.GetPresignedUrlAsync(
                _storageOptions.Bucket,
                objectKey,
                ttl,
                ct
            );
        }

        return await _objectStorage.GetPresignedUrlHeaderAsync(
            _storageOptions.Bucket,
            objectKey,
            ttl,
            headers,
            ct
        );
    }

    public async Task<string> GetPresignedPreviewUrlAsync(
        string id,
        TimeSpan expiry,
        CancellationToken ct = default
    )
    {
        var doc = await _procDocumentRepository.GetByIdAsync(id);
        if (doc is null || string.IsNullOrWhiteSpace(doc.ObjectKey))
            throw new KeyNotFoundException("Dokumen tidak ditemukan.");

        return await GetPresignedViewUrlByObjectKeyAsync(
            doc.ObjectKey,
            doc.FileName,
            doc.ContentType,
            expiry,
            ct
        );
    }
}
