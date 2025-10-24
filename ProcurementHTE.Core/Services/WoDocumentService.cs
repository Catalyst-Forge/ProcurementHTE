using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

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
        string woDocumentId, TimeSpan ttl, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(woDocumentId)
                  ?? throw new InvalidOperationException("Dokumen tidak ditemukan.");

        if (ttl <= TimeSpan.Zero)
            ttl = TimeSpan.FromSeconds(_opts.PresignExpirySeconds);

        // Hanya ambil URL presign — tanpa menambah query lagi
        var url = await _storage.GetPresignedUrlAsync(_opts.Bucket, doc.ObjectKey, ttl);
        return url;
    }

    public async Task<string> GetPresignedPreviewUrlAsync(
        string woDocumentId, TimeSpan expiry, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(woDocumentId)
                  ?? throw new InvalidOperationException("Dokumen tidak ditemukan.");

        if (expiry <= TimeSpan.Zero)
            expiry = TimeSpan.FromSeconds(_opts.PresignExpirySeconds);

        var url = await _storage.GetPresignedUrlAsync(_opts.Bucket, doc.ObjectKey, expiry);
        return url;
    }


}
