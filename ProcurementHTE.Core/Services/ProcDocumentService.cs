using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using QRCoder;

namespace ProcurementHTE.Core.Services;

public sealed class ProcDocumentService : IProcDocumentService
{
    private readonly IProcDocumentRepository _procDocumentRepository;
    private readonly IProcurementRepository _procurementRepository;
    private readonly IJobTypeDocumentRepository _jobTypeDocumentRepository;
    private readonly IProcDocApprovalFlowService _approvalFlowService;
    private readonly IProfitLossService _pnlService;
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IObjectStorage _objectStorage;
    private readonly ObjectStorageOptions _storageOptions;

    public ProcDocumentService(
        IProcDocumentRepository procDocumentRepository,
        IProcurementRepository procurementRepository,
        IJobTypeDocumentRepository jobTypeDocumentRepository,
        IProcDocApprovalFlowService approvalFlowService,
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
        _approvalFlowService =
            approvalFlowService ?? throw new ArgumentNullException(nameof(approvalFlowService));
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

    public async Task<bool> DeleteAsync(string procDocumentId)
    {
        var entity = await _procDocumentRepository.GetByIdAsync(procDocumentId);
        if (entity is null)
            return false;

        if (!string.IsNullOrWhiteSpace(entity.ObjectKey))
        {
            await SafeDeleteAsync(entity.ObjectKey);
        }

        if (!string.IsNullOrWhiteSpace(entity.QrObjectKey))
        {
            await SafeDeleteAsync(entity.QrObjectKey);
        }

        await _procDocumentRepository.DeleteAsync(procDocumentId);
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
            Status = DocStatuses.Uploaded,
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
            ct
        );

        var docs = await _procDocumentRepository.GetByProcurementAsync(procurementId);
        var availableDocTypes = docs.Where(d =>
                !string.Equals(d.Status, DocStatuses.Deleted, StringComparison.OrdinalIgnoreCase)
            )
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

    public async Task SendApprovalAsync(
        string procDocumentId,
        string requestedByUserId,
        CancellationToken ct = default
    )
    {
        var doc = await _procDocumentRepository.GetByIdAsync(procDocumentId);
        if (doc == null)
            throw new InvalidOperationException("Dokumen tidak ditemukan.");
        if (string.Equals(doc.Status, DocStatuses.Deleted, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Dokumen sudah dihapus.");

        var procurement = await GetProcurementOrThrowAsync(doc.ProcurementId);
        var jobTypeDocs = string.IsNullOrWhiteSpace(procurement.JobTypeId)
            ? Array.Empty<JobTypeDocuments>()
            : await _jobTypeDocumentRepository.ListByJobTypeAsync(procurement.JobTypeId, ct);

        await EnsureQrArtifactsAsync(doc, procurement, ct);

        var config = jobTypeDocs.FirstOrDefault(j =>
            j.DocumentTypeId.Equals(doc.DocumentTypeId, StringComparison.OrdinalIgnoreCase)
        );

        if (config?.RequiresApproval == true)
        {
            if (
                !string.Equals(
                    doc.Status,
                    DocStatuses.PendingApproval,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                // Special handling: if this document is Profit & Loss and selected final offer > 300,000,000
                // then append Vice President to approval flow
                var extraRoles = new List<string>();
                try
                {
                    var docType = await _documentTypeRepository.GetByIdAsync(doc.DocumentTypeId);
                    if (
                        docType != null
                        && !string.IsNullOrWhiteSpace(docType.Name)
                        && docType.Name.IndexOf(
                            "Profit & Loss.pdf",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0
                    )
                    {
                        var pnl = await _pnlService.GetLatestByProcurementAsync(doc.ProcurementId);
                        if (pnl != null && pnl.SelectedVendorFinalOffer > 300_000_000m)
                        {
                            extraRoles.Add("Vice President");
                        }
                    }
                }
                catch (Exception ex) { }

                if (extraRoles.Count > 0)
                    await _approvalFlowService.GenerateFlowAsync(
                        doc.ProcurementId,
                        doc.ProcDocumentId,
                        extraRoles
                    );
                else
                    await _approvalFlowService.GenerateFlowAsync(
                        doc.ProcurementId,
                        doc.ProcDocumentId
                    );

                doc.Status = DocStatuses.PendingApproval;
                await _procDocumentRepository.UpdateAsync(doc);
            }
        }
        else
        {
            if (!DocStatuses.IsFinal(doc.Status ?? string.Empty))
            {
                doc.Status = DocStatuses.Approved;
                doc.IsApproved = true;
                doc.ApprovedAt = DateTime.UtcNow;
                doc.ApprovedByUserId = requestedByUserId;
                await _procDocumentRepository.UpdateAsync(doc);
            }
        }

        await _procDocumentRepository.SaveAsync();
    }

    public async Task<string?> GetPresignedQrUrlAsync(
        string procDocumentId,
        TimeSpan expiry,
        CancellationToken ct = default
    )
    {
        var doc = await _procDocumentRepository.GetByIdAsync(procDocumentId);
        if (doc is null)
            return null;

        var procurement = await GetProcurementOrThrowAsync(doc.ProcurementId);
        await EnsureQrArtifactsAsync(doc, procurement, ct);
        await _procDocumentRepository.SaveAsync();

        if (string.IsNullOrWhiteSpace(doc.QrObjectKey))
            return null;

        return await _objectStorage.GetPresignedUrlAsync(
            _storageOptions.Bucket,
            doc.QrObjectKey,
            expiry,
            ct
        );
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
                Status = DocStatuses.Uploaded,
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
            entity.Status = DocStatuses.Uploaded;
            entity.IsApproved = null;
            entity.ApprovedAt = null;
            entity.ApprovedByUserId = null;
            entity.QrText = null;
            entity.QrObjectKey = null;

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

    private static string BuildQrObjectKey(string procurementId, string procDocumentId) =>
        $"procurements/{procurementId}/qr/{procDocumentId}.png";

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

    private static byte[] GenerateQrBytes(string data)
    {
        var generator = new QRCodeGenerator();
        var qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(pixelsPerModule: 10);
    }

    private static string BuildQrText(ProcDocuments doc, Procurement procurement)
    {
        return $"DocId={doc.ProcDocumentId};ProcNum={procurement.ProcNum};Type={doc.DocumentTypeId}";
    }

    private async Task EnsureQrArtifactsAsync(
        ProcDocuments doc,
        Procurement procurement,
        CancellationToken ct
    )
    {
        var requiresUpload = string.IsNullOrWhiteSpace(doc.QrObjectKey);
        if (string.IsNullOrWhiteSpace(doc.QrText))
        {
            doc.QrText = BuildQrText(doc, procurement);
            await _procDocumentRepository.UpdateAsync(doc);
        }

        if (!requiresUpload)
            return;

        var bytes = GenerateQrBytes(doc.QrText!);
        var objectKey = BuildQrObjectKey(procurement.ProcurementId, doc.ProcDocumentId);

        using (var ms = new MemoryStream(bytes, writable: false))
        {
            await _objectStorage.UploadAsync(
                _storageOptions.Bucket,
                objectKey,
                ms,
                ms.Length,
                "image/png",
                ct
            );
        }

        doc.QrObjectKey = objectKey;
        await _procDocumentRepository.UpdateAsync(doc);
    }

    private async Task SafeDeleteAsync(string objectKey)
    {
        try
        {
            await _objectStorage.DeleteAsync(_storageOptions.Bucket, objectKey);
        }
        catch (Exception ex) { }
    }

    #endregion
}
