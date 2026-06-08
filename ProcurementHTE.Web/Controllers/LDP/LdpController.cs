using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Ldp
{
    [Authorize]
    public partial class LdpController : Controller
    {
        private readonly ILdpService _ldpService;
        private readonly IProcurementRepository _procurementRepo;
        private readonly IObjectStorage _objectStorage;
        private readonly ILogger<LdpController> _logger;
        private readonly string _bucketName;

        public LdpController(
            ILdpService ldpService,
            IProcurementRepository procurementRepo,
            IObjectStorage objectStorage,
            ILogger<LdpController> logger,
            IOptions<ObjectStorageOptions> storageOptions)
        {
            _ldpService = ldpService ?? throw new ArgumentNullException(nameof(ldpService));
            _procurementRepo = procurementRepo ?? throw new ArgumentNullException(nameof(procurementRepo));
            _objectStorage = objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bucketName = storageOptions.Value.Bucket;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 25,
            string? search = null,
            CancellationToken ct = default
        )
        {
            try
            {
                var (items, totalCount) = await _ldpService.GetAllAsync(page, pageSize, search, ct);

                var viewModel = new LdpIndexViewModel
                {
                    Items = items,
                    Total = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading LDP data");
                TempData["Error"] = "Terjadi kesalahan saat memuat data LDP.";
                return View(new LdpIndexViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            string procurementId,
            IFormFile ldpFile,
            CancellationToken ct)
        {
            bool isAjax = Request.Headers.XRequestedWith == "XMLHttpRequest"
                       || Request.Headers.Accept.ToString().Contains("application/json")
                       || Request.Headers.ContentType.ToString().Contains("multipart/form-data");

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    if (isAjax) return BadRequest(new { success = false, message = "User tidak teridentifikasi." });
                    TempData["Error"] = "User tidak teridentifikasi.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate file is provided
                if (ldpFile == null || ldpFile.Length == 0)
                {
                    if (isAjax) return BadRequest(new { success = false, message = "File dokumen LDP wajib diupload." });
                    TempData["Error"] = "File dokumen LDP wajib diupload.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate file type (PDF only)
                var ext = Path.GetExtension(ldpFile.FileName).ToLowerInvariant();
                var isPdf = ext == ".pdf" || ldpFile.ContentType.ToLower() == "application/pdf";
                if (!isPdf)
                {
                    if (isAjax) return BadRequest(new { success = false, message = "Format file LDP harus PDF." });
                    TempData["Error"] = "Format file LDP harus PDF.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate file size (max 50MB)
                const long maxFileSize = 50 * 1024 * 1024; // 50MB
                if (ldpFile.Length > maxFileSize)
                {
                    if (isAjax) return BadRequest(new { success = false, message = "Ukuran file LDP maksimal 50MB." });
                    TempData["Error"] = "Ukuran file LDP maksimal 50MB.";
                    return RedirectToAction(nameof(Index));
                }

                // Get procurement
                var procurement = await _procurementRepo.GetByIdAsync(procurementId);
                if (procurement == null)
                {
                    if (isAjax) return NotFound(new { success = false, message = "Procurement tidak ditemukan." });
                    TempData["Error"] = "Procurement tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                // Upload file to object storage
                var objectKey = $"procurements/{procurementId}/ldp/{Guid.NewGuid():N}-{ldpFile.FileName}";
                await using var stream = ldpFile.OpenReadStream();
                await _objectStorage.UploadAsync(
                    _bucketName,
                    objectKey,
                    stream,
                    ldpFile.Length,
                    ldpFile.ContentType,
                    ct
                );

                // Update procurement with LDP file info
                procurement.LdpFileName = ldpFile.FileName;
                procurement.LdpFileObjectKey = objectKey;
                procurement.LdpFileContentType = ldpFile.ContentType;
                procurement.LdpFileSize = ldpFile.Length;
                procurement.LdpUploadedAt = DateTime.UtcNow;
                procurement.LdpUploadedByUserId = userId;
                procurement.UpdatedAt = DateTime.UtcNow;

                await _procurementRepo.UpdateProcurementAsync(procurement);

                if (isAjax) return Ok(new { success = true, message = "Dokumen LDP berhasil diupload." });
                TempData["Success"] = "Dokumen LDP berhasil diupload.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading LDP document for procurement {ProcurementId}", procurementId);
                if (isAjax) return StatusCode(500, new { success = false, message = "Terjadi kesalahan saat mengupload dokumen LDP." });
                TempData["Error"] = "Terjadi kesalahan saat mengupload dokumen LDP.";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
