using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public sealed class ProcDocumentService : IProcDocumentService
{
    private readonly IProcDocumentRepository _procDocumentRepository;
    private readonly IProcurementRepository _procurementRepository;
    private readonly IJobTypeDocumentRepository _jobTypeDocumentRepository;
    private readonly IProfitLossService _pnlService;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IObjectStorage _objectStorage;
    private readonly ObjectStorageOptions _storageOptions;

    public ProcDocumentService(
        IProcDocumentRepository procDocumentRepository,
        IProcurementRepository procurementRepository,
        IJobTypeDocumentRepository jobTypeDocumentRepository,
        IProfitLossService pnlService,
        IDocumentTypeRepository documentTypeRepository,
        IObjectStorage objectStorage,
        IOptions<ObjectStorageOptions> storageOptions
    )
    {
        _procDocumentRepository =
            procDocumentRepository
            ?? throw new ArgumentNullException(nameof(procDocumentRepository));
        _procurementRepository =
            procurementRepository ?? throw new ArgumentNullException(nameof(procurementRepository));
        _jobTypeDocumentRepository =
            jobTypeDocumentRepository
            ?? throw new ArgumentNullException(nameof(jobTypeDocumentRepository));
        _pnlService = pnlService ?? throw new ArgumentNullException(nameof(pnlService));
        _documentTypeRepository =
            documentTypeRepository
            ?? throw new ArgumentNullException(nameof(documentTypeRepository));
        _objectStorage = objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
        _storageOptions =
            storageOptions?.Value ?? throw new ArgumentNullException(nameof(storageOptions));

        if (string.IsNullOrWhiteSpace(_storageOptions.Bucket))
            throw new ArgumentException(
                "Object storage bucket belum dikonfigurasi.",
                nameof(storageOptions)
            );
    }

    #region Public API

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
        {
            await SafeDeleteAsync(entity.ObjectKey);
        }

        await _procDocumentRepository.DeleteAsync(procDocumentId, deletedByUserId);
        await _procDocumentRepository.SaveAsync();

        return true;
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

    public async Task<bool> CanSendApprovalAsync(
        string procurementId,
        CancellationToken ct = default
    )
    {
        var procurement = await GetProcurementOrThrowAsync(procurementId);
        if (string.IsNullOrWhiteSpace(procurement.JobTypeId))
            return false;

        var jobTypeDocs = await _jobTypeDocumentRepository.ListByJobTypeAsync(
            procurement.JobTypeId,
            procurement.ProcurementCategory,
            ct
        );

        var docs = await _procDocumentRepository.GetByProcurementAsync(procurementId);
        var availableDocTypes = docs
            .Select(d => d.DocumentTypeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (jobTypeDocs.Count == 0)
            return availableDocTypes.Count > 0;

        var required = jobTypeDocs
            .Where(d => d.IsMandatory || d.IsUploadRequired || d.RequiresApproval)
            .ToList();

        if (required.Count == 0)
            return availableDocTypes.Count > 0;

        return required.All(cfg => availableDocTypes.Contains(cfg.DocumentTypeId));
    }

    /// <summary>
    /// Send approval sekarang hanya generate approval flow jika diperlukan.
    /// Status tracking ada di level PR, bukan per document.
    /// </summary>
    public async Task SendApprovalAsync(
        string procDocumentId,
        string requestedByUserId,
        CancellationToken ct = default
    )
    {
        var doc = await _procDocumentRepository.GetByIdAsync(procDocumentId);
        if (doc == null)
            throw new InvalidOperationException("Dokumen tidak ditemukan.");

        // SPH / SNH tidak memerlukan approval: skip
        try
        {
            var docType = await _documentTypeRepository.GetByIdAsync(doc.DocumentTypeId);
            var name = docType?.Name ?? doc.DocumentType?.Name ?? string.Empty;
            var fileName = doc.FileName ?? string.Empty;
            if (
                name.Equals("Surat Penawaran Harga", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Surat Negosiasi Harga", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("SPH_", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("SNH_", StringComparison.OrdinalIgnoreCase)
                || (fileName.Contains("Penawaran", StringComparison.OrdinalIgnoreCase)
                    && fileName.Contains("Harga", StringComparison.OrdinalIgnoreCase))
            )
            {
                return; // No approval needed for SPH/SNH
            }
        }
        catch (Exception)
        {
            // fallback ke flow biasa jika lookup docType gagal
        }

        var procurement = await GetProcurementOrThrowAsync(doc.ProcurementId);
        var jobTypeDocs = string.IsNullOrWhiteSpace(procurement.JobTypeId)
            ? Array.Empty<JobTypeDocuments>()
            : await _jobTypeDocumentRepository.ListByJobTypeAsync(
                procurement.JobTypeId,
                procurement.ProcurementCategory,
                ct
            );

        var config = jobTypeDocs.FirstOrDefault(j =>
            j.DocumentTypeId.Equals(doc.DocumentTypeId, StringComparison.OrdinalIgnoreCase)
        );

        // Approval flow per document sudah dihapus
        // Approval sekarang hanya di level PR
    }

    /// <summary>
    /// QR Code sekarang ada di level PR, bukan per document.
    /// Method ini deprecated dan hanya return null.
    /// </summary>
    public Task<string?> GetPresignedQrUrlAsync(
        string procDocumentId,
        TimeSpan expiry,
        CancellationToken ct = default
    )
    {
        return Task.FromResult<string?>(null);
    }

    public async Task<UploadProcDocumentResult> SaveGeneratedAsync(
        GeneratedProcDocumentRequest request,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Bytes is null || request.Bytes.Length == 0)
            throw new ArgumentException("File hasil generate kosong.", nameof(request.Bytes));

        _ = await GetProcurementOrThrowAsync(request.ProcurementId);

        ProcDocuments? existingDoc = null;
        string? previousObjectKey = null;
        if (!string.IsNullOrWhiteSpace(request.ProcDocumentId))
        {
            existingDoc = await _procDocumentRepository.GetByIdAsync(request.ProcDocumentId);
            if (existingDoc is null)
                throw new KeyNotFoundException(
                    $"ProcDocument dengan ID '{request.ProcDocumentId}' tidak ditemukan."
                );

            if (
                !string.Equals(
                    existingDoc.ProcurementId,
                    request.ProcurementId,
                    StringComparison.Ordinal
                )
            )
                throw new InvalidOperationException(
                    "ProcDocument tidak sesuai dengan procurement yang dimaksud."
                );

            previousObjectKey = existingDoc.ObjectKey;
        }

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

        ProcDocuments entity;
        if (existingDoc is null)
        {
            entity = new ProcDocuments
            {
                ProcurementId = request.ProcurementId,
                DocumentTypeId = request.DocumentTypeId,
                FileName = request.FileName,
                ObjectKey = objectKey,
                ContentType = request.ContentType,
                Size = request.Bytes.Length,
                Description = request.Description,
                CreatedAt = request.CreatedAt == default ? DateTime.UtcNow : request.CreatedAt,
                CreatedByUserId = request.GeneratedByUserId,
            };

            await _procDocumentRepository.AddAsync(entity);
        }
        else
        {
            entity = existingDoc;
            entity.DocumentTypeId = request.DocumentTypeId;
            entity.FileName = request.FileName;
            entity.ObjectKey = objectKey;
            entity.ContentType = request.ContentType;
            entity.Size = request.Bytes.Length;
            entity.Description = request.Description;
            entity.CreatedAt = request.CreatedAt == default ? DateTime.UtcNow : request.CreatedAt;
            entity.CreatedByUserId = request.GeneratedByUserId;

            await _procDocumentRepository.UpdateAsync(entity);
        }

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

    #endregion

    #region Helpers

    private async Task<Procurement> GetProcurementOrThrowAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ProcurementId tidak boleh kosong.", nameof(procurementId));

        var procurement = await _procurementRepository.GetByIdAsync(procurementId);
        return procurement
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID '{procurementId}' tidak ditemukan."
            );
    }

    private TimeSpan GetExpiry(TimeSpan? overrideValue)
    {
        if (overrideValue.HasValue && overrideValue.Value > TimeSpan.Zero)
            return overrideValue.Value;

        var seconds = Math.Max(60, _storageOptions.PresignExpirySeconds);
        return TimeSpan.FromSeconds(seconds);
    }

    private static string BuildDocumentObjectKey(
        string procurementId,
        string documentTypeId,
        string fileName
    )
    {
        var sanitized = SanitizeFileName(fileName);
        return $"procurements/{procurementId}/documents/{documentTypeId}/{Guid.NewGuid():N}-{sanitized}";
    }

    private static string BuildGeneratedObjectKey(
        string procurementId,
        string documentTypeId,
        string fileName
    ) =>
        $"procurements/{procurementId}/generated/{documentTypeId}/{Guid.NewGuid():N}-{SanitizeFileName(fileName)}";

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file.dat";

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var ch in fileName.Trim())
        {
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        }

        return builder.ToString();
    }

    private async Task SafeDeleteAsync(string objectKey)
    {
        try
        {
            await _objectStorage.DeleteAsync(_storageOptions.Bucket, objectKey);
        }
        catch (Exception) { }
    }

    #endregion
}
