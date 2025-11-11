using System.IO;
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
    IProfitLossService pnlService,
    IWoTypeDocumentRepository wtdConfigRepo,
    IWoDocumentApprovalRepository approvalRepo,
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
    private readonly IProfitLossService _pnlService = pnlService;
    private readonly IWoTypeDocumentRepository _wtdConfigRepo = wtdConfigRepo;
    private readonly IWoDocumentApprovalRepository _approvalRepo = approvalRepo;
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

        await _storage.DeleteAsync(_opts.Bucket, doc.ObjectKey);
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

            // 1) Validasi WorkOrder
            var wo =
                await _woRepo.GetByIdAsync(request.WorkOrderId)
                ?? throw new InvalidOperationException("Work Order tidak ditemukan.");

            // 2) Validasi DocumentType
            var docType =
                await _docTypeRepo.GetByIdAsync(request.DocumentTypeId)
                ?? throw new InvalidOperationException("DocumentType tidak ditemukan.");

            // 3) Validasi WoTypeDocuments
            if (string.IsNullOrWhiteSpace(wo.WoTypeId))
                throw new InvalidOperationException("Work Order belum memiliki WoType.");

            var wtd =
                await _wtdRepo.GetByWoTypeAndDocumentTypeAsync(wo.WoTypeId!, request.DocumentTypeId)
                ?? throw new InvalidOperationException(
                    "Dokumen ini tidak terdaftar pada WoType terkait."
                );

            if (!wtd.IsUploadRequired)
                throw new InvalidOperationException("Dokumen ini tidak memerlukan upload.");

            // 4) Path object
            var now = request.NowUtc;
            var safeWoType = SlugFolderHelper.SlugFolder(wo.WoType!.TypeName);
            var safeDocType = SlugFolderHelper.SlugFolder(docType.Name);
            var guid = Guid.NewGuid().ToString("N");

            var finalDbFileName = $"{docType.Name}.pdf";
            var finalObjectFile = $"{guid}_{wo.WoNum}.pdf";
            var objectKey =
                $"{safeWoType}/{safeDocType}/{now:yyyy}/{now:MM}/{now:dd}/{finalObjectFile}";

            _logger.LogInformation("[UploadWoDoc] ObjectKey={Key}", objectKey);

            // 5) Upload ke Object Storage
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

            // 6) Simpan metadata
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
            throw;
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

        return await _storage.GetPresignedUrlAsync(_opts.Bucket, doc.ObjectKey, ttl);
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

        return await _storage.GetPresignedUrlAsync(_opts.Bucket, doc.ObjectKey, expiry);
    }

    public async Task<bool> CanSendApprovalAsync(string workOrderId, CancellationToken ct = default)
    {
        var wo =
            await _woRepo.GetByIdAsync(workOrderId)
            ?? throw new InvalidOperationException("Work Order tidak ditemukan.");
        if (string.IsNullOrWhiteSpace(wo.WoTypeId))
            throw new InvalidOperationException("Work Order belum memiliki WoType.");

        // 1) Ambil semua konfigurasi untuk WoType terkait
        var configs = await _wtdRepo.ListByWoTypeAsync(wo.WoTypeId!, ct);

        // 2) Ambil hanya dokumen yang WAJIB (IsMandatory) dan PERLU APPROVAL (RequiresApproval)
        var mustHaveDocTypeIds = configs
            .Where(c => c.IsMandatory && c.RequiresApproval)
            .Select(c => c.DocumentTypeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Jika tidak ada yang mandatory+requiresApproval, kirim approval dianggap tidak relevan
        if (mustHaveDocTypeIds.Count == 0)
        {
            _logger.LogInformation(
                "[CanSendApproval] WO={WO} tidak punya dokumen mandatory+approval.",
                workOrderId
            );
            return false;
        }

        // 3) Lihat WoDocuments yang aktif (bukan Deleted) untuk WO ini
        var activeDocs = (await _woDocRepo.GetByWorkOrderAsync(workOrderId))
            .Where(d => !string.Equals(d.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var haveDocTypeIds = activeDocs
            .Select(d => d.DocumentTypeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 4) Semua mandatory+approval harus sudah ada (apapun statusnya: Uploaded/Generated/Pending/Approved/Rejected/Replaced, asal bukan Deleted)
        bool allPresent = mustHaveDocTypeIds.IsSubsetOf(haveDocTypeIds);

        if (!allPresent)
        {
            var missing = mustHaveDocTypeIds.Where(dt => !haveDocTypeIds.Contains(dt)).ToArray();
            _logger.LogWarning(
                "[CanSendApproval] WO={WO} belum lengkap. Missing mandatory+approval: {Count} -> {List}",
                workOrderId,
                missing.Length,
                string.Join(", ", missing)
            );
        }
        else
        {
            _logger.LogInformation(
                "[CanSendApproval] WO={WO} siap dikirim. Total mandatory+approval={Count}",
                workOrderId,
                mustHaveDocTypeIds.Count
            );
        }

        return allPresent;
    }

    public async Task SendApprovalAsync(
        string workOrderId,
        string requestedByUserId,
        CancellationToken ct = default
    )
    {
        // 0) Pastikan mandatory+approval lengkap
        if (!await CanSendApprovalAsync(workOrderId, ct))
            throw new InvalidOperationException("Dokumen wajib belum lengkap.");

        // 1) Ambil WO
        var wo =
            await _woRepo.GetByIdAsync(workOrderId)
            ?? throw new InvalidOperationException("Work Order tidak ditemukan.");
        if (string.IsNullOrWhiteSpace(wo.WoTypeId))
            throw new InvalidOperationException("Work Order belum memiliki WoType.");

        // 2) Muat semua dokumen aktif (non-Deleted)
        var allDocs = await _woDocRepo.GetByWorkOrderAsync(workOrderId);
        var docs = allDocs
            .Where(d => !string.Equals(d.Status, "Deleted", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // 3) Rule VP
        const decimal ThresholdVP = 500_000_000m;
        var pnl = await _pnlService.GetLatestByWorkOrderAsync(workOrderId);
        var needVP = (pnl?.SelectedVendorFinalOffer ?? 0m) > ThresholdVP;

        _logger.LogInformation(
            "[SendApproval] WO={WO} Docs: total={Total}, active={Active}, needVP={NeedVP}",
            workOrderId,
            allDocs.Count,
            docs.Count,
            needVP
        );

        var nowUtc = DateTime.UtcNow;
        var approvalsToInsert = new List<WoDocumentApprovals>();

        // === KUNCI: Proses per-DocumentTypeId, pilih champion terbaru saja ===
        var groups = docs.GroupBy(d => d.DocumentTypeId, StringComparer.OrdinalIgnoreCase);
        foreach (var g in groups)
        {
            // ---- Pilih champion (dokumen terbaru) ----
            var champion = g.OrderByDescending(d => d.CreatedAt).First();

            // ---- Turunkan older yang keburu Pending Approval agar tidak dobel indeks unik ----
            var olderPendings = g.Where(x =>
                    x.WoDocumentId != champion.WoDocumentId
                    && string.Equals(
                        x.Status,
                        "Pending Approval",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .ToList();

            if (olderPendings.Count > 0)
            {
                foreach (var old in olderPendings)
                {
                    old.Status = "Replaced"; // atau "Superseded" sesuai konvensi kamu
                    await _woDocRepo.UpdateAsync(old);
                }
                // Save dulu untuk memutus siklus dependency di batch ini
                await _woDocRepo.SaveAsync();
            }

            // ---- Cek WTD champion ----
            var wtdConfig = await _wtdConfigRepo.FindByWoTypeAndDocTypeAsync(
                wo.WoTypeId!,
                champion.DocumentTypeId
            );
            if (wtdConfig is null)
            {
                _logger.LogInformation(
                    "[SendApproval] SKIP Doc={DocId} DT={DT} : no WTD config",
                    champion.WoDocumentId,
                    champion.DocumentTypeId
                );
                continue;
            }
            if (!wtdConfig.RequiresApproval)
            {
                _logger.LogInformation(
                    "[SendApproval] SKIP Doc={DocId} DT={DT} : RequiresApproval=false",
                    champion.WoDocumentId,
                    champion.DocumentTypeId
                );
                continue;
            }

            // ---- Susun chain + filter VP ----
            var chain = (wtdConfig.DocumentApprovals ?? [])
                .OrderBy(a => a.Level)
                .ThenBy(a => a.SequenceOrder)
                .ToList();

            var chainBefore = chain.Count;
            if (!needVP)
            {
                chain = chain
                    .Where(a =>
                        a.Role != null
                        && !string.Equals(
                            a.Role.Name,
                            "Vice President",
                            StringComparison.OrdinalIgnoreCase
                        )
                        && !a.Role.Name.Contains("vice", StringComparison.OrdinalIgnoreCase)
                        && !a.Role.Name.Contains("vp", StringComparison.OrdinalIgnoreCase)
                        && !a.Role.Name.Contains(
                            "wakil presiden",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .OrderBy(a => a.Level)
                    .ThenBy(a => a.SequenceOrder)
                    .ToList();
            }
            if (chain.Count == 0)
            {
                _logger.LogInformation(
                    "[SendApproval] SKIP Doc={DocId} DT={DT} : chain empty after VP filter (before={Before})",
                    champion.WoDocumentId,
                    champion.DocumentTypeId,
                    chainBefore
                );
                continue;
            }

            // ---- Idempoten: tambah step yang belum ada saja ----
            var existing = await _approvalRepo.GetByWoDocumentIdAsync(champion.WoDocumentId);
            var existingKeys = existing.Select(e => (e.Level, e.SequenceOrder)).ToHashSet();

            var firstLevel = chain.Min(c => c.Level);
            var firstSeqOnFirstLevel = chain
                .Where(c => c.Level == firstLevel)
                .Min(c => c.SequenceOrder);
            int addNow = 0;

            foreach (var step in chain)
            {
                if (existingKeys.Contains((step.Level, step.SequenceOrder)))
                    continue;

                approvalsToInsert.Add(
                    new WoDocumentApprovals
                    {
                        WorkOrderId = workOrderId,
                        WoDocumentId = champion.WoDocumentId,
                        RoleId = step.RoleId,
                        Level = step.Level,
                        SequenceOrder = step.SequenceOrder,
                        Status =
                            (step.Level == firstLevel && step.SequenceOrder == firstSeqOnFirstLevel)
                                ? "Pending"
                                : "Blocked",
                    }
                );
                addNow++;
            }

            // ---- Pastikan QR ada ----
            bool docChanged = false;
            if (string.IsNullOrWhiteSpace(champion.QrObjectKey))
            {
                var payload =
                    $"WO={wo.WoNum};DocType={champion.DocumentTypeId};DocId={champion.WoDocumentId};ts={nowUtc:o}";
                var qrGen = new QRCodeGenerator();
                var qrData = qrGen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                var pngQr = new PngByteQRCode(qrData);
                var qrBytes = pngQr.GetGraphic(20);

                var baseKey = string.IsNullOrWhiteSpace(champion.ObjectKey)
                    ? $"wo/{workOrderId}/{champion.WoDocumentId}"
                    : champion.ObjectKey.Replace(".pdf", "", StringComparison.Ordinal);

                var qrKey = $"{baseKey}_QR.png";
                await using (var ms = new MemoryStream(qrBytes))
                    await _storage.UploadAsync(_opts.Bucket, qrKey, ms, ms.Length, "image/png", ct);

                champion.QrText = payload;
                champion.QrObjectKey = qrKey;
                docChanged = true;
            }

            // ---- Set status champion -> Pending Approval (hanya champion) ----
            bool hasPending =
                existing.Any(e =>
                    string.Equals(e.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                )
                || approvalsToInsert.Any(a =>
                    a.WoDocumentId == champion.WoDocumentId
                    && string.Equals(a.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                );

            if (
                hasPending
                && !string.Equals(
                    champion.Status,
                    "Pending Approval",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                champion.Status = "Pending Approval";
                docChanged = true;
            }

            if (docChanged)
            {
                await _woDocRepo.UpdateAsync(champion);
                // Save per-group untuk mencegah siklus di batch besar
                await _woDocRepo.SaveAsync();
            }

            _logger.LogInformation(
                "[SendApproval] Doc={DocId} DT={DT} chain(before={Before}, after={After}) existing={Existing} addNow={AddNow} championStatus={Status}",
                champion.WoDocumentId,
                champion.DocumentTypeId,
                chainBefore,
                chain.Count,
                existing.Count,
                addNow,
                champion.Status
            );
        }

        // 4) Commit steps approval
        if (approvalsToInsert.Count > 0)
            await _approvalRepo.AddRangeAsync(approvalsToInsert);

        await _approvalRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[SendApproval] WO={WO} DONE: insertedSteps={Inserted}",
            workOrderId,
            approvalsToInsert.Count
        );
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

    public async Task<UploadWoDocumentResult> SaveGeneratedAsync(
        GeneratedWoDocumentRequest request,
        CancellationToken ct = default
    )
    {
        var wo =
            await _woRepo.GetByIdAsync(request.WorkOrderId)
            ?? throw new InvalidOperationException("Work order tidak ditemukan");

        if (string.IsNullOrWhiteSpace(wo.WoTypeId))
            throw new InvalidOperationException("Work Order belum memiliki WoType.");

        var wtd =
            await _wtdRepo.GetByWoTypeAndDocumentTypeAsync(wo.WoTypeId!, request.DocumentTypeId)
            ?? throw new InvalidOperationException(
                "Dokumen ini tidak terdaftar pada WoType terkait"
            );

        if (!wtd.IsGenerated)
            throw new InvalidOperationException(
                "Dokumen ini bukan dokumen yang digenerate oleh sistem"
            );

        var now = request.CreatedAt;
        var safeWoType = SlugFolderHelper.SlugFolder(wo.WoType!.TypeName);
        var safeDocType = SlugFolderHelper.SlugFolder(wtd.DocumentType.Name);
        var guid = Guid.NewGuid().ToString("N");
        var objectKey =
            $"{safeWoType}/{safeDocType}/{now:yyyy}/{now:MM}/{now:dd}/{guid}_{wo.WoNum}.pdf";

        await using var ms = new MemoryStream(request.Bytes);
        await _storage.UploadAsync(_opts.Bucket, objectKey, ms, ms.Length, request.ContentType, ct);

        var last = await _woDocRepo.GetLatestActiveByWorkOrderAndDocTypeAsync(
            request.WorkOrderId,
            request.DocumentTypeId
        );
        if (last is not null && last.Status != "Deleted")
        {
            last.Status = "Replaced";
            await _woDocRepo.UpdateAsync(last);
        }

        var entity = new WoDocuments
        {
            WorkOrderId = request.WorkOrderId,
            DocumentTypeId = request.DocumentTypeId,
            FileName = request.FileName,
            ObjectKey = objectKey,
            ContentType = request.ContentType,
            Size = request.Bytes.LongLength,
            Status = "Generated",
            Description = request.Description,
            CreatedAt = now,
            CreatedByUserId = request.GeneratedByUserId,
        };

        await _woDocRepo.AddAsync(entity);
        await _woDocRepo.SaveAsync();

        return new UploadWoDocumentResult
        {
            WoDocumentId = entity.WoDocumentId,
            ObjectKey = objectKey,
            FileName = entity.FileName,
            Size = entity.Size,
        };
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
