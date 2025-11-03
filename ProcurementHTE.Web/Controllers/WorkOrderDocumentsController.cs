using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;
using System.Security.Claims;

namespace ProcurementHTE.Web.Controllers;

[Authorize]
public class WorkOrderDocumentsController : Controller
{
    private readonly IWorkOrderDocumentQuery _query;
    private readonly IWoDocumentService _docSvc;
    private readonly ILogger<WorkOrderDocumentsController> _logger;
    private readonly IWorkOrderService _woService;
    private readonly IHttpClientFactory _http;
    private readonly IDocumentGenerator _docGenerator;
    private readonly IDocumentTypeRepository _docTypeRepo;

    public WorkOrderDocumentsController(
        IWorkOrderDocumentQuery query,
        IWorkOrderService woService,
        IWoDocumentService docSvc,
        ILogger<WorkOrderDocumentsController> logger,
        IHttpClientFactory http,
        IDocumentGenerator docGenerator,
        IDocumentTypeRepository docTypeRepo
    )
    {
        _query = query;
        _docSvc = docSvc;
        _logger = logger;
        _woService = woService;
        _http = http;
        _docGenerator = docGenerator;
        _docTypeRepo = docTypeRepo;
    }

    // GET: /WorkOrderDocuments/Index/{workOrderId}
    [HttpGet("WorkOrderDocuments/Index/{workOrderId}")]
    public async Task<IActionResult> Index(string workOrderId)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
        {
            _logger.LogWarning("[WODocs] Index without workOrderId.");
            return BadRequest("Parameter workOrderId tidak valid.");
        }

        try
        {
            var dto = await _query.GetRequiredDocsAsync(workOrderId, TimeSpan.FromMinutes(30));
            if (dto is null)
            {
                _logger.LogInformation("[WODocs] WO {WO} tidak ditemukan.", workOrderId);
                return NotFound("Work Order tidak ditemukan.");
            }

            var wonum = await _woService.GetWorkOrderByIdAsync(workOrderId);
            ViewBag.WoNum = wonum?.WoNum ?? "-";
            var vm = new WorkOrderRequiredDocsVm
            {
                WorkOrderId = dto.WorkOrderId,
                WoTypeId = dto.WoTypeId,
                Items =
                [
                    .. dto.Items.Select(x => new RequiredDocItemDto
                    {
                        WoTypeDocumentId = x.WoTypeDocumentId,
                        Sequence = x.Sequence,
                        DocumentTypeId = x.DocumentTypeId,
                        DocumentTypeName = x.DocumentTypeName,
                        IsMandatory = x.IsMandatory,
                        IsUploadRequired = x.IsUploadRequired,
                        IsGenerated = x.IsGenerated,
                        RequiresApproval = x.RequiresApproval,
                        Note = x.Note,
                        Uploaded = x.Uploaded,
                        WoDocumentId = x.WoDocumentId,
                        FileName = x.FileName,
                        Size = x.Size,
                        Status = x.Status,
                    }),
                ],
            };

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] Error load Index for WO={WO}.", workOrderId);
            TempData["error"] = "Gagal memuat daftar dokumen.";
            return RedirectToAction("Error", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 25L * 1024 * 1024)]
    [RequestSizeLimit(25L * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        string WorkOrderId,
        string DocumentTypeId,
        IFormFile File,
        string? Description
    )
    {
        if (string.IsNullOrWhiteSpace(WorkOrderId) || string.IsNullOrWhiteSpace(DocumentTypeId))
        {
            TempData["error"] = "Parameter tidak lengkap.";
            return RedirectToAction(nameof(Index), new { workOrderId = WorkOrderId });
        }

        if (File is null || File.Length == 0)
        {
            TempData["error"] = "File belum dipilih.";
            return RedirectToAction(nameof(Index), new { workOrderId = WorkOrderId });
        }

        // opsional: enforce PDF
        var contentType = (File.ContentType ?? "").ToLowerInvariant();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            await using var stream = File.OpenReadStream();
            var req = new UploadWoDocumentRequest
            {
                WorkOrderId = WorkOrderId,
                DocumentTypeId = DocumentTypeId,
                Content = stream,
                Size = File.Length,
                FileName = File.FileName,
                ContentType = contentType,
                Description = Description,
                UploadedByUserId = userId,
                NowUtc = DateTime.UtcNow,
            };

            var result = await _docSvc.UploadAsync(req, HttpContext.RequestAborted);
            TempData["success"] = $"Berhasil upload “{File.FileName}”.";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[WODocs] Upload gagal: WO={WO}, DocType={DT}, File={FN}",
                WorkOrderId,
                DocumentTypeId,
                File?.FileName
            );
            TempData["error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { workOrderId = WorkOrderId });
    }

    // GET: /WorkOrderDocuments/Download/{id}?workOrderId=WO123
    [HttpGet("WorkOrderDocuments/Download/{id}")]
    public async Task<IActionResult> Download(string id, string? workOrderId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        try
        {
            var doc = await _docSvc.GetByIdAsync(id);
            if (doc is null)
                return NotFound();

            var url = await _docSvc.GetPresignedDownloadUrlAsync(
                id,
                TimeSpan.FromMinutes(30),
                HttpContext.RequestAborted
            );

            var client = _http.CreateClient("MinioProxy");
            var resp = await client.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                HttpContext.RequestAborted
            );

            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync(HttpContext.RequestAborted);

            // Kembalikan stream langsung; browser akan "Save As" karena header dari File() di bawah
            var contentType = string.IsNullOrWhiteSpace(doc.ContentType)
                ? "application/octet-stream"
                : doc.ContentType;

            // enableRangeProcessing true kalau mau dukung resume/dll
            return File(
                stream,
                contentType,
                fileDownloadName: doc.FileName,
                enableRangeProcessing: true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] Download gagal: id={Id}", id);
            TempData["error"] = "Gagal mengunduh dokumen.";
            return RedirectToAction(nameof(Index), new { workOrderId });
        }
    }

    // GET: /WorkOrderDocuments/Preview/{id}?workOrderId=WO123
    [HttpGet("WorkOrderDocuments/PreviewUrl/{id}")]
    public async Task<IActionResult> PreviewUrl(string id, string? workOrderId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();
        try
        {
            var url = await _docSvc.GetPresignedPreviewUrlAsync(
                id,
                TimeSpan.FromMinutes(15),
                HttpContext.RequestAborted
            );
            return Json(new { ok = true, url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] PreviewUrl gagal: id={Id}", id);
            return Json(new { ok = false, error = "Gagal membuat link preview." });
        }
    }

    // POST: /WorkOrderDocuments/Delete/{id}
    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] string id, [FromForm] string workOrderId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        try
        {
            var ok = await _docSvc.DeleteAsync(id);
            TempData[ok ? "success" : "error"] = ok
                ? "Dokumen dihapus."
                : "Dokumen tidak ditemukan.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] Delete gagal: id={Id}", id);
            TempData["error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { workOrderId });
    }

    // POST: /WorkOrderDocuments/SendApproval
    [HttpPost("SendApproval")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendApproval([FromForm] string workOrderId)
    {
        try
        {
            var userId = User?.Identity?.Name ?? "-";
            var ok = await _docSvc.CanSendApprovalAsync(workOrderId);
            if (!ok)
            {
                TempData["error"] = "Dokumen wajib belum lengkap.";
                return RedirectToAction(nameof(Index), new { workOrderId });
            }

            await _docSvc.SendApprovalAsync(workOrderId, userId, HttpContext.RequestAborted);
            TempData["success"] =
                "Dokumen dikirim untuk approval. Status menjadi Pending Approval.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] SendApproval gagal untuk WO={WO}", workOrderId);
            TempData["error"] = "Gagal mengirim approval.";
        }

        return RedirectToAction(nameof(Index), new { workOrderId });
    }

    [HttpGet("WorkOrderDocuments/QrUrl/{id}")]
    public async Task<IActionResult> QrUrl(string id)
    {
        try
        {
            var url = await _docSvc.GetPresignedQrUrlAsync(
                id,
                TimeSpan.FromMinutes(15),
                HttpContext.RequestAborted
            );
            return Json(new { ok = url != null, url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] QrUrl gagal: id={Id}", id);
            return Json(new { ok = false, error = "Gagal membuat link QR." });
        }
    }

    [HttpGet("WorkOrderDocuments/DownloadQr/{id}")]
    public async Task<IActionResult> DownloadQr(string id)
    {
        try
        {
            var doc = await _docSvc.GetByIdAsync(id);
            if (doc is null || string.IsNullOrWhiteSpace(doc.QrObjectKey))
                return NotFound();

            var url = await _docSvc.GetPresignedQrUrlAsync(
                id,
                TimeSpan.FromMinutes(30),
                HttpContext.RequestAborted
            );

            var client = _http.CreateClient("MinioProxy"); // sama seperti download file
            var resp = await client.GetAsync(
                url!,
                HttpCompletionOption.ResponseHeadersRead,
                HttpContext.RequestAborted
            );
            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
            return File(stream, "image/png", fileDownloadName: Path.GetFileName(doc.QrObjectKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] DownloadQr gagal: id={Id}", id);
            return BadRequest("Gagal mengunduh QR.");
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(string workOrderId, string documentTypeId) {
        try {
            var wo = await _woService.GetWorkOrderByIdAsync(workOrderId);
            if (wo == null) {
                TempData["error"] = "Work Order tidak ditemukan";
                return RedirectToAction("Index", new { workOrderId });
            }

            var docType = await _docTypeRepo.GetByIdAsync(documentTypeId);
            if (docType == null) {
                TempData["error"] = "Document Type tidak ditemukan";
                return RedirectToAction("Index", new { workOrderId });
            }

            // Generate PDF berdasarkan nama dokumen
            byte[] pdfBytes = docType.Name switch {
                "Memorandum" => await _docGenerator.GenerateMemorandumAsync(wo),
                "Permintaan Pekerjaan" => await _docGenerator.GeneratePermintaanPekerjaanAsync(wo),
                "Service Order" => await _docGenerator.GenerateServiceOrderAsync(wo),
                "Market Survey" => await _docGenerator.GenerateMarketSurveyAsync(wo),
                "Surat Perintah Mulai Pekerjaan (SPMP)" => await _docGenerator.GenerateSPMPAsync(wo),
                "Surat Penawaran Harga" => await _docGenerator.GenerateSuratPenawaranHargaAsync(wo),
                "Surat Negosiasi Harga" => await _docGenerator.GenerateSuratNegosiasiHargaAsync(wo),
                "Rencana Kerja dan Syarat-Syarat (RKS)" => await _docGenerator.GenerateRKSAsync(wo),
                "Risk Assessment (RA)" => await _docGenerator.GenerateRiskAssessmentAsync(wo),
                "Owner Estimate (OE)" => await _docGenerator.GenerateOwnerEstimateAsync(wo),
                "Bill of Quantity (BOQ)" => await _docGenerator.GenerateBOQAsync(wo),
                _ => throw new NotImplementedException($"Template untuk '{docType.Name}' belum tersedia")
            };

            // Simpan ke MinIO via existing service
            var result = await _docSvc.SaveGeneratedAsync(new GeneratedWoDocumentRequest {
                WorkOrderId = workOrderId,
                DocumentTypeId = documentTypeId,
                Bytes = pdfBytes,
                FileName = $"{docType.Name}.pdf",
                ContentType = "application/pdf",
                Description = $"Generated from template on {DateTime.Now:dd MMM yyyy HH:mm}",
                CreatedAt = DateTime.UtcNow,
                GeneratedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            });

            TempData["success"] = $"Dokumen '{docType.Name}' berhasil digenerate!";
            _logger.LogInformation(
                "Document generated: WO={WO}, DocType={DocType}, Size={Size}",
                wo.WoNum, docType.Name, pdfBytes.Length
            );

            return RedirectToAction("Index", new { workOrderId });
        } catch (NotImplementedException ex) {
            TempData["error"] = ex.Message;
            _logger.LogWarning(ex, "Template not implemented for DocumentTypeId={DocTypeId}", documentTypeId);
            return RedirectToAction("Index", new { workOrderId });
        } catch (Exception ex) {
            TempData["error"] = $"Gagal generate dokumen: {ex.Message}";
            _logger.LogError(ex, "Error generating document for WO={WO}", workOrderId);
            return RedirectToAction("Index", new { workOrderId });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> PreviewGenerated(string workOrderId, string documentTypeId) {
        try {
            var wo = await _woService.GetWorkOrderByIdAsync(workOrderId);
            if (wo == null)
                return NotFound("Work Order tidak ditemukan");

            var docType = await _docTypeRepo.GetByIdAsync(documentTypeId);
            if (docType == null)
                return NotFound("Document Type tidak ditemukan");

            byte[] pdfBytes = docType.Name switch {
                "Memorandum" => await _docGenerator.GenerateMemorandumAsync(wo),
                "Permintaan Pekerjaan" => await _docGenerator.GeneratePermintaanPekerjaanAsync(wo),
                "Service Order" => await _docGenerator.GenerateServiceOrderAsync(wo),
                "Market Survey" => await _docGenerator.GenerateMarketSurveyAsync(wo),
                "Surat Perintah Mulai Pekerjaan (SPMP)" => await _docGenerator.GenerateSPMPAsync(wo),
                "Surat Penawaran Harga" => await _docGenerator.GenerateSuratPenawaranHargaAsync(wo),
                "Surat Negosiasi Harga" => await _docGenerator.GenerateSuratNegosiasiHargaAsync(wo),
                "Rencana Kerja dan Syarat-Syarat (RKS)" => await _docGenerator.GenerateRKSAsync(wo),
                "Risk Assessment (RA)" => await _docGenerator.GenerateRiskAssessmentAsync(wo),
                "Owner Estimate (OE)" => await _docGenerator.GenerateOwnerEstimateAsync(wo),
                "Bill of Quantity (BOQ)" => await _docGenerator.GenerateBOQAsync(wo),
                _ => throw new NotImplementedException($"Template untuk '{docType.Name}' belum tersedia")
            };

            return File(pdfBytes, "application/pdf", $"{docType.Name}_Preview.pdf");
        } catch (Exception ex) {
            _logger.LogError(ex, "Error previewing generated document");
            return BadRequest(new { error = ex.Message });
        }
    }
}
