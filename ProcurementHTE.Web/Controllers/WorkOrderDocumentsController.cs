using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers;

[Authorize]
public class WorkOrderDocumentsController : Controller
{
    private readonly IWorkOrderDocumentQuery _query;
    private readonly IWoDocumentService _docSvc;
    private readonly ILogger<WorkOrderDocumentsController> _logger;
    private readonly IWorkOrderService _woService;

    public WorkOrderDocumentsController(
        IWorkOrderDocumentQuery query,
        IWorkOrderService woService,
        IWoDocumentService docSvc,
        ILogger<WorkOrderDocumentsController> logger)
    {
        _query = query;
        _docSvc = docSvc;
        _logger = logger;
        _woService = woService;
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
                Items = [.. dto.Items.Select(x => new RequiredDocItemDto
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
                    Size = x.Size
                })]
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
    public async Task<IActionResult> Upload(string WorkOrderId, string DocumentTypeId, IFormFile File, string? Description)
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
                UploadedByUserId = User?.Identity?.Name,
                NowUtc = DateTime.UtcNow
            };

            var result = await _docSvc.UploadAsync(req, HttpContext.RequestAborted);
            TempData["success"] = $"Berhasil upload “{File.FileName}”.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] Upload gagal: WO={WO}, DocType={DT}, File={FN}", WorkOrderId, DocumentTypeId, File?.FileName);
            TempData["error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { workOrderId = WorkOrderId });
    }

    // GET: /WorkOrderDocuments/Download/{id}?workOrderId=WO123
    [HttpGet("WorkOrderDocuments/Download/{id}")]
    public async Task<IActionResult> Download(string id, string? workOrderId)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        try
        {
            var url = await _docSvc.GetPresignedDownloadUrlAsync(id, TimeSpan.FromMinutes(30), HttpContext.RequestAborted);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] Download gagal: id={Id}", id);
            TempData["error"] = "Gagal membuat link download.";
            return RedirectToAction(nameof(Index), new { workOrderId });
        }
    }

    // GET: /WorkOrderDocuments/Preview/{id}?workOrderId=WO123
    [HttpGet("WorkOrderDocuments/PreviewUrl/{id}")]
    public async Task<IActionResult> PreviewUrl(string id, string? workOrderId)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();
        try
        {
            var url = await _docSvc.GetPresignedPreviewUrlAsync(id, TimeSpan.FromMinutes(15), HttpContext.RequestAborted);
            return Json(new { ok = true, url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] PreviewUrl gagal: id={Id}", id);
            return Json(new { ok = false, error = "Gagal membuat link preview." });
        }
    }


    // POST: /WorkOrderDocuments/Delete/{id}
    [HttpPost("WorkOrderDocuments/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string workOrderId)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        try
        {
            var ok = await _docSvc.DeleteAsync(id);
            TempData[ok ? "success" : "error"] = ok ? "Dokumen dihapus." : "Dokumen tidak ditemukan.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WODocs] Delete gagal: id={Id}", id);
            TempData["error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { workOrderId });
    }
}
