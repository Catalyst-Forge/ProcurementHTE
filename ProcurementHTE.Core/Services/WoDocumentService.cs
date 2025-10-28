using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using QRCoder;

namespace ProcurementHTE.Core.Services;

public class WoDocumentService(
    IWoDocumentRepository repo,
    IObjectStorage storage,
    IOptions<ObjectStorageOptions> minioOptions,
    IWorkOrderRepository woRepo,
    IWoTypeDocumentRepository woTypeDocumentRepository,
    IDocumentTypeRepository documentTypeRepository,
    IWoDocumentRepository woDocumentRepository,
    ILogger<WoDocumentService> logger
) : IWoDocumentService
{
    private readonly IWoDocumentRepository _repo = repo;
    private readonly IObjectStorage _storage = storage;
    private readonly ObjectStorageOptions _opts = minioOptions.Value;

    private readonly IWorkOrderRepository _woRepo = woRepo;
    private readonly IWoTypeDocumentRepository _wtdRepo = woTypeDocumentRepository;
    private readonly IDocumentTypeRepository _docTypeRepo = documentTypeRepository;
    private readonly IWoDocumentRepository _woDocRepo = woDocumentRepository;
    private readonly ILogger<WoDocumentService> _logger = logger;

    public Task<WoDocuments?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);

    public Task<IReadOnlyList<WoDocuments>> ListByWorkOrderAsync(string workOrderId) =>
        _repo.GetByWorkOrderAsync(workOrderId);

    public async Task<string?> GetPresignedUrlAsync(string woDocumentId, TimeSpan? expires = null)
    {
        var doc = await _repo.GetByIdAsync(woDocumentId);
        if (doc == null || doc.Status == "Deleted")
            return null;
        return await _storage.GetPresignedUrlAsync(
            _opts.Bucket,
            doc.ObjectKey,
            expires ?? TimeSpan.FromSeconds(_opts.PresignExpirySeconds)
        );
    }

    public async Task<bool> DeleteAsync(string woDocumentId)
    {
        var doc = await _repo.GetByIdAsync(woDocumentId);
        if (doc == null)
            return false;

        // hapus objek di MinIO (opsional: soft-delete saja)
        await _storage.DeleteAsync(_opts.Bucket, doc.ObjectKey);

        // tandai di DB
        await _repo.DeleteAsync(woDocumentId);
        await _repo.SaveAsync();
        return true;
    }

    public async Task<UploadWoDocumentResult> UploadAsync(
        UploadWoDocumentRequest request,
        CancellationToken ct = default
    )
    {
        try
        {
            _logger.LogInformation(
                "[UploadWoDoc] Mulai upload WO={WO}, DocType={DT}, User={User}",
                request.WorkOrderId,
                request.DocumentTypeId,
                request.UploadedByUserId
            );

            // 1️⃣ Validasi WorkOrder
            var wo =
                await _woRepo.GetByIdAsync(request.WorkOrderId)
                ?? throw new InvalidOperationException("Work Order tidak ditemukan.");

            // 2️⃣ Validasi DocumentType
            var docType =
                await _docTypeRepo.GetByIdAsync(request.DocumentTypeId)
                ?? throw new InvalidOperationException("DocumentType tidak ditemukan.");

            // 3️⃣ Validasi WoTypeDocuments
            var wtd =
                await _wtdRepo.GetByWoTypeAndDocumentTypeAsync(wo.WoTypeId!, request.DocumentTypeId)
                ?? throw new InvalidOperationException(
                    "Dokumen ini tidak terdaftar pada WoType terkait."
                );
            if (!wtd.IsUploadRequired)
                throw new InvalidOperationException("Dokumen ini tidak memerlukan upload.");

            // 4️⃣ Bangun path file
            var now = request.NowUtc;
            var safeWoType = SlugFolderHelper.SlugFolder(wo.WoType!.TypeName);
            var safeDocType = SlugFolderHelper.SlugFolder(docType.Name);
            var guid = Guid.NewGuid().ToString("N");

            var finalDbFileName = $"{docType.Name}.pdf";
            var finalObjectFile = $"{guid}_{wo.WoNum}.pdf";
            var objectKey =
                $"{safeWoType}/{safeDocType}/{now:yyyy}/{now:MM}/{now:dd}/{finalObjectFile}";

            _logger.LogInformation("[UploadWoDoc] ObjectKey={Key}", objectKey);

            // 5️⃣ Upload ke MinIO
            try
            {
                await _storage.UploadAsync(
                    _opts.Bucket,
                    objectKey,
                    request.Content,
                    request.Size,
                    request.ContentType,
                    ct
                );
                _logger.LogInformation("[UploadWoDoc] Upload ke MinIO sukses: {Key}", objectKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[UploadWoDoc] Gagal upload ke MinIO. Bucket={Bucket}, Key={Key}",
                    _opts.Bucket,
                    objectKey
                );
                throw new InvalidOperationException(
                    $"Gagal upload ke object storage: {ex.Message}",
                    ex
                );
            }

            // 6️⃣ Simpan metadata ke DB
            try
            {
                var entity = new WoDocuments
                {
                    WorkOrderId = request.WorkOrderId,
                    DocumentTypeId = request.DocumentTypeId,
                    FileName = finalDbFileName,
                    ObjectKey = objectKey,
                    ContentType = request.ContentType,
                    Size = request.Size,
                    Status = "Uploaded",
                    Description = request.Description,
                    CreatedAt = now,
                    CreatedByUserId = request.UploadedByUserId,
                };

                await _woDocRepo.AddAsync(entity);
                await _woDocRepo.SaveAsync();

                _logger.LogInformation(
                    "[UploadWoDoc] Metadata tersimpan: WoDocumentId={Id}, Size={Size}",
                    entity.WoDocumentId,
                    entity.Size
                );

                return new UploadWoDocumentResult
                {
                    WoDocumentId = entity.WoDocumentId,
                    ObjectKey = objectKey,
                    FileName = entity.FileName,
                    Size = entity.Size,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[UploadWoDoc] Gagal simpan metadata ke DB (WO={WO}, DocType={DT})",
                    request.WorkOrderId,
                    request.DocumentTypeId
                );
                throw new InvalidOperationException(
                    "Gagal menyimpan metadata dokumen ke database.",
                    ex
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[UploadWoDoc] Error umum saat upload WO={WO}, DocType={DT}",
                request.WorkOrderId,
                request.DocumentTypeId
            );
            throw; // biar tetap muncul di controller (TempData["error"])
        }
    }

    public async Task<string> GetPresignedDownloadUrlAsync(
        string woDocumentId,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        var doc =
            await _repo.GetByIdAsync(woDocumentId)
            ?? throw new InvalidOperationException("Dokumen tidak ditemukan.");

        if (ttl <= TimeSpan.Zero)
            ttl = TimeSpan.FromSeconds(_opts.PresignExpirySeconds);

        // Hanya ambil URL presign — tanpa menambah query lagi
        var url = await _storage.GetPresignedUrlAsync(_opts.Bucket, doc.ObjectKey, ttl);
        return url;
    }

    public async Task<string> GetPresignedPreviewUrlAsync(
        string woDocumentId,
        TimeSpan expiry,
        CancellationToken ct = default
    )
    {
        var doc =
            await _repo.GetByIdAsync(woDocumentId)
            ?? throw new InvalidOperationException("Dokumen tidak ditemukan.");

        if (expiry <= TimeSpan.Zero)
            expiry = TimeSpan.FromSeconds(_opts.PresignExpirySeconds);

        var url = await _storage.GetPresignedUrlAsync(_opts.Bucket, doc.ObjectKey, expiry);
        return url;
    }

    public async Task<bool> CanSendApprovalAsync(string workOrderId)
    {
        // 1) Ambil WO
        var wo =
            await _woRepo.GetByIdAsync(workOrderId)
            ?? throw new InvalidOperationException("Work Order tidak ditemukan.");

        if (string.IsNullOrWhiteSpace(wo.WoTypeId))
            throw new InvalidOperationException("Work Order belum memiliki WoType.");

        // 2) Ambil semua konfigurasi dokumen untuk WoType tsb, lalu filter yang wajib upload
        var requiredDocTypeIds = (await _wtdRepo.ListByWoTypeAsync(wo.WoTypeId))
            .Where(x => x.IsUploadRequired)
            .Select(x => x.DocumentTypeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requiredDocTypeIds.Count == 0)
            return false; // tidak ada yang wajib => tidak perlu send approval

        // 3) Ambil semua dokumen yang sudah ter-upload untuk WO ini (abaikan yang Deleted)
        var uploaded = await _woDocRepo.GetByWorkOrderAsync(workOrderId);
        var uploadedDocTypeIds = uploaded
            .Where(d => !string.Equals(d.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.DocumentTypeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 4) Valid: semua doc type yang wajib sudah ada di uploaded?
        return requiredDocTypeIds.IsSubsetOf(uploadedDocTypeIds);
    }

    public async Task SendApprovalAsync(
        string workOrderId,
        string requestedByUserId,
        CancellationToken ct = default
    )
    {
        if (!await CanSendApprovalAsync(workOrderId))
            throw new InvalidOperationException("Dokumen wajib belum lengkap.");

        var wo =
            await _woRepo.GetByIdAsync(workOrderId)
            ?? throw new InvalidOperationException("Work Order tidak ditemukan.");

        // Ambil semua dokumen terakhir per DocumentType utk WO ini
        var docs = await _woDocRepo.GetByWorkOrderAsync(workOrderId);

        var now = DateTime.UtcNow;
        foreach (var doc in docs.Where(d => d.Status != "Deleted"))
        {
            // 1) Generate payload & gambar QR (PNG) per dokumen
            var payload =
                $"WO={wo.WoNum};DocType={doc.DocumentTypeId};DocId={doc.WoDocumentId};ts={now:o}";
            using var qrGen = new QRCodeGenerator();
            using var data = qrGen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            using var qr = new QRCode(data);
            using var bmp = qr.GetGraphic(10);
            await using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            // 2) Upload PNG ke MinIO (pakai folder yg sama dgn file dokumennya)
            var qrKey = doc.ObjectKey.Replace(".pdf", "") + "_QR.png";
            await _storage.UploadAsync(_opts.Bucket, qrKey, ms, ms.Length, "image/png", ct);

            // 3) Update metadata dokumen
            doc.QrText = payload;
            doc.QrObjectKey = qrKey;
            doc.Status = "Pending Approval"; // status transisi
            await _woDocRepo.UpdateAsync(doc);
        }

        await _woDocRepo.SaveAsync();
    }

    public async Task<string?> GetPresignedQrUrlAsync(
        string woDocumentId,
        TimeSpan expiry,
        CancellationToken ct = default
    )
    {
        var doc = await _repo.GetByIdAsync(woDocumentId);
        if (doc == null || string.IsNullOrWhiteSpace(doc.QrObjectKey))
            return null;

        if (expiry <= TimeSpan.Zero)
            expiry = TimeSpan.FromSeconds(_opts.PresignExpirySeconds);

        return await _storage.GetPresignedUrlAsync(_opts.Bucket, doc.QrObjectKey, expiry, ct);
    }

    public async Task<string> GetPresignedViewUrlByObjectKeyAsync(
        string objectKey,
        string? fileName,
        string? contentType,
        TimeSpan ttl,
        CancellationToken ct = default
    )
    {
        if (ttl <= TimeSpan.Zero)
            ttl = TimeSpan.FromSeconds(_opts.PresignExpirySeconds);

        var headers = new Dictionary<string, string>
        {
            ["response-content-disposition"] =
                $"inline; filename=\"{(string.IsNullOrWhiteSpace(fileName) ? "document.pdf" : fileName)}\"",
            ["response-content-type"] = string.IsNullOrWhiteSpace(contentType)
                ? "application/pdf"
                : contentType,
        };

        return await _storage.GetPresignedUrlHeaderAsync(_opts.Bucket, objectKey, ttl, headers, ct);
    }
}
