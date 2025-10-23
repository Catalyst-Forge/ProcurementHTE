using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client.Extensions.Msal;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Web.Models.ViewModels;


namespace ProcurementHTE.Web.Controllers;

[Authorize]
public class WorkOrderDocumentsController : Controller
{
    private readonly IWorkOrderDocumentQuery _query;
    private readonly IWoDocumentService _docSvc;
    private readonly ILogger<WorkOrderDocumentsController> _logger;


    public WorkOrderDocumentsController(
        IWorkOrderDocumentQuery query, 
        IWoDocumentService docSvc,
        ILogger<WorkOrderDocumentsController> logger)
    {
        _query = query;
        _docSvc = docSvc;
        _logger = logger;
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

            _logger.LogInformation("[WODocs] VM Items Count: {Count} (WO={WO}, WoTypeId={Type})",
                dto.Items.Count, dto.WorkOrderId, dto.WoTypeId);

            var vm = new WorkOrderRequiredDocsVm
            {
                WorkOrderId = dto.WorkOrderId,
                WoTypeId = dto.WoTypeId,
                Items = dto.Items.Select(x => new RequiredDocItemDto
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
                }).ToList()
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
        if (File is null || File.Length == 0)
        {
            TempData["error"] = "File belum dipilih.";
            return RedirectToAction(nameof(Index), new { workOrderId = WorkOrderId });
        }

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
                ContentType = string.IsNullOrWhiteSpace(File.ContentType) ? "application/octet-stream" : File.ContentType,
                Description = Description,
                UploadedByUserId = User?.Identity?.Name
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

    [HttpGet]
    public async Task<IActionResult> Download(string id)
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
            return RedirectToAction(nameof(Index)); // opsional tambahkan workOrderId if available
        }
    }

}
