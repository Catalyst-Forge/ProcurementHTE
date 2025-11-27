using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

[Authorize]
public class ProcurementDocumentsController : Controller
{
    private readonly IProcurementDocumentQuery _query;
    private readonly IProcDocumentService _docSvc;
    private readonly IProcurementService _procurementService;
    private readonly IHttpClientFactory _http;
    private readonly IDocumentGenerator _docGenerator;
    private readonly IDocumentTypeRepository _docTypeRepo;
    private readonly IApprovalService _approvalSvc;

    public ProcurementDocumentsController(
        IProcurementDocumentQuery query,
        IProcurementService procurementService,
        IProcDocumentService docSvc,
        IHttpClientFactory http,
        IDocumentGenerator docGenerator,
        IDocumentTypeRepository docTypeRepo,
        IApprovalService approvalSvc
    )
    {
        _query = query;
        _docSvc = docSvc;
        _procurementService = procurementService;
        _http = http;
        _docGenerator = docGenerator;
        _docTypeRepo = docTypeRepo;
        _approvalSvc = approvalSvc;
    }

    // GET: /ProcurementDocuments/Index/{procurementId}
    [HttpGet("ProcurementDocuments/Index/{procurementId}")]
    public async Task<IActionResult> Index(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
        {
            return BadRequest("Invalid procurementId parameter.");
        }

        try
        {
            var dto = await _query.GetRequiredDocsAsync(procurementId, TimeSpan.FromMinutes(30));
            if (dto is null)
            {
                return NotFound("Procurement was not found.");
            }

            var procurement = await _procurementService.GetProcurementByIdAsync(procurementId);
            ViewBag.ProcNum = procurement?.ProcNum ?? "-";
            var vm = new ProcurementRequiredDocsVm
            {
                ProcurementId = dto.ProcurementId,
                JobTypeId = dto.JobTypeId,
                Items =
                [
                    .. dto.Items.Select(x => new RequiredDocItemDto
                    {
                        JobTypeDocumentId = x.JobTypeDocumentId,
                        Sequence = x.Sequence,
                        DocumentTypeId = x.DocumentTypeId,
                        DocumentTypeName = x.DocumentTypeName,
                        IsMandatory = x.IsMandatory,
                        IsUploadRequired = x.IsUploadRequired,
                        IsGenerated = x.IsGenerated,
                        RequiresApproval = x.RequiresApproval,
                        Note = x.Note,
                        Uploaded = x.Uploaded,
                        ProcDocumentId = x.ProcDocumentId,
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
            TempData["ErrorMessage"] = "Failed to load document list.";
            return RedirectToAction("Index", "Error");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 25L * 1024 * 1024)]
    [RequestSizeLimit(25L * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        string ProcurementId,
        string DocumentTypeId,
        IFormFile File,
        string? Description
    )
    {
        if (string.IsNullOrWhiteSpace(ProcurementId) || string.IsNullOrWhiteSpace(DocumentTypeId))
        {
            TempData["ErrorMessage"] = "Missing required parameters.";
            return RedirectToAction(nameof(Index), new { procurementId = ProcurementId });
        }

        if (File is null || File.Length == 0)
        {
            TempData["ErrorMessage"] = "No file selected.";
            return RedirectToAction(nameof(Index), new { procurementId = ProcurementId });
        }

        var ext = (Path.GetExtension(File.FileName) ?? string.Empty).ToLowerInvariant();
        var isPdf = ext == ".pdf";
        if (!isPdf)
        {
            TempData["ErrorMessage"] = "Only PDF files are allowed.";
            return RedirectToAction(nameof(Index), new { procurementId = ProcurementId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var docType = await _docTypeRepo.GetByIdAsync(DocumentTypeId);
            var baseName = SanitizeFileNameBase(docType?.Name) ?? DocumentTypeId;

            Stream uploadStream;
            long uploadSize;
            string uploadFileName;
            string uploadContentType;

            uploadStream = File.OpenReadStream();
            uploadSize = File.Length;
            uploadFileName = $"{baseName}.pdf";
            uploadContentType = "application/pdf";

            await using var stream = uploadStream;
            var req = new UploadProcDocumentRequest
            {
                ProcurementId = ProcurementId,
                DocumentTypeId = DocumentTypeId,
                Content = stream,
                Size = uploadSize,
                FileName = uploadFileName,
                ContentType = uploadContentType,
                Description = Description,
                UploadedByUserId = userId,
                NowUtc = DateTime.UtcNow,
            };

            var result = await _docSvc.UploadAsync(req, HttpContext.RequestAborted);
            var message = $"Successfully uploaded \"{uploadFileName}\".";

            if (IsAjaxRequest())
            {
                return Json(
                    new
                    {
                        ok = true,
                        message,
                        procurementId = ProcurementId,
                        documentTypeId = DocumentTypeId,
                        document = new
                        {
                            id = result.ProcDocumentId,
                            name = result.FileName,
                            size = result.Size,
                        },
                    }
                );
            }

            TempData["SuccessMessage"] = message;
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new { ok = false, error = ex.Message });
            }

            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { procurementId = ProcurementId });
    }

    private bool IsAjaxRequest()
    {
        if (Request is null)
            return false;
        if (
            Request.Headers.TryGetValue("X-Requested-With", out var requestedWith)
            && requestedWith == "XMLHttpRequest"
        )
        {
            return true;
        }

        if (
            Request.Headers.TryGetValue("Accept", out var acceptHeader)
            && acceptHeader.Any(h =>
                h != null && h.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return true;
        }

        return false;
    }

    // GET: /ProcurementDocuments/Download/{id}?procurementId=WO123
    [HttpGet("ProcurementDocuments/Download/{id}")]
    public async Task<IActionResult> Download(string id, string? procurementId)
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
            TempData["ErrorMessage"] = "Failed to download document.";
            return RedirectToAction(nameof(Index), new { procurementId });
        }
    }

    // GET: /ProcurementDocuments/Preview/{id}?procurementId=WO123
    [HttpGet("ProcurementDocuments/PreviewUrl/{id}")]
    public async Task<IActionResult> PreviewUrl(string id, string? procurementId)
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
            return Json(new { ok = false, error = "Failed to create preview link." });
        }
    }

    // POST: /ProcurementDocuments/Delete/{id}
    [HttpPost("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] string id, [FromForm] string procurementId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        try
        {
            var ok = await _docSvc.DeleteAsync(id);
            TempData[ok ? "success" : "error"] = ok ? "Document deleted." : "Document not found.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { procurementId });
    }

    // POST: /ProcurementDocuments/SendApprovalPerDoc
    [HttpPost("SendApprovalPerDoc")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendApprovalPerDoc(
        [FromForm] string procDocumentId,
        [FromForm] string procurementId
    )
    {
        if (string.IsNullOrWhiteSpace(procDocumentId))
        {
            TempData["ErrorMessage"] = "Invalid document.";
            return RedirectToAction(nameof(Index), new { procurementId });
        }
        try
        {
            var userId = User?.Identity?.Name ?? "-";
            await _docSvc.SendApprovalAsync(procDocumentId, userId, HttpContext.RequestAborted);
            TempData["SuccessMessage"] =
                "Dokumen dikirim untuk approval. Status menjadi Pending Approval.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { procurementId });
    }

    [HttpGet("ProcurementDocuments/QrUrl/{id}")]
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
            return Json(new { ok = false, error = "Failed to create QR link." });
        }
    }

    [HttpGet("ProcurementDocuments/DownloadQr/{id}")]
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
            return BadRequest("Failed to download QR.");
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(
        string procurementId,
        string documentTypeId,
        string? procDocumentId
    )
    {
        try
        {
            var procurementEntity = await _procurementService.GetProcurementByIdAsync(
                procurementId
            );
            if (procurementEntity == null)
            {
                TempData["ErrorMessage"] = "Procurement was not found";
                return RedirectToAction("Index", new { procurementId });
            }

            var docType = await _docTypeRepo.GetByIdAsync(documentTypeId);
            if (docType == null)
            {
                TempData["ErrorMessage"] = "Document type was not found";
                return RedirectToAction("Index", new { procurementId });
            }

            // Generate PDF berdasarkan nama dokumen
            byte[] pdfBytes = docType.Name switch
            {
                "Memorandum" => await _docGenerator.GenerateMemorandumAsync(procurementEntity),
                "Permintaan Pekerjaan" => await _docGenerator.GeneratePermintaanPekerjaanAsync(
                    procurementEntity
                ),
                "Service Order" => await _docGenerator.GenerateServiceOrderAsync(procurementEntity),
                "Market Survey" => await _docGenerator.GenerateMarketSurveyAsync(procurementEntity),
                "Surat Perintah Mulai Pekerjaan (SPMP)" => await _docGenerator.GenerateSPMPAsync(
                    procurementEntity
                ),
                "Surat Penawaran Harga" => await _docGenerator.GenerateSuratPenawaranHargaAsync(
                    procurementEntity
                ),
                "Surat Negosiasi Harga" => await _docGenerator.GenerateSuratNegosiasiHargaAsync(
                    procurementEntity
                ),
                "Rencana Kerja dan Syarat-Syarat (RKS)" => await _docGenerator.GenerateRKSAsync(
                    procurementEntity
                ),
                "Risk Assessment (RA)" => await _docGenerator.GenerateRiskAssessmentAsync(
                    procurementEntity
                ),
                "Owner Estimate (OE)" => await _docGenerator.GenerateOwnerEstimateAsync(
                    procurementEntity
                ),
                "Bill of Quantity (BOQ)" => await _docGenerator.GenerateBOQAsync(procurementEntity),
                "Profit & Loss" => await _docGenerator.GenerateProfitLossAsync(procurementEntity),
                _ => throw new NotImplementedException(
                    $"Template untuk '{docType.Name}' belum tersedia"
                ),
            };

            // Simpan ke MinIO via existing service
            var result = await _docSvc.SaveGeneratedAsync(
                new GeneratedProcDocumentRequest
                {
                    ProcurementId = procurementId,
                    DocumentTypeId = documentTypeId,
                    Bytes = pdfBytes,
                    FileName = $"{docType.Name}.pdf",
                    ContentType = "application/pdf",
                    Description = $"Generated from template on {DateTime.Now:dd MMM yyyy HH:mm}",
                    CreatedAt = DateTime.UtcNow,
                    GeneratedByUserId = User.FindFirst(
                        System.Security.Claims.ClaimTypes.NameIdentifier
                    )?.Value,
                    ProcDocumentId = procDocumentId,
                }
            );

            TempData["SuccessMessage"] = $"Dokumen '{docType.Name}' berhasil digenerate!";

            return RedirectToAction("Index", new { procurementId });
        }
        catch (NotImplementedException ex)
        {
            TempData["ErrorMessage"] = ex.Message;

            return RedirectToAction("Index", new { procurementId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to generate documents: {ex.Message}";

            return RedirectToAction("Index", new { procurementId });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> PreviewGenerated(string procurementId, string documentTypeId)
    {
        try
        {
            var procurement = await _procurementService.GetProcurementByIdAsync(procurementId);
            if (procurement == null)
                return NotFound("Procurement was not found");

            var docType = await _docTypeRepo.GetByIdAsync(documentTypeId);
            if (docType == null)
                return NotFound("Document type was not found");

            byte[] pdfBytes = docType.Name switch
            {
                "Memorandum" => await _docGenerator.GenerateMemorandumAsync(procurement),
                "Permintaan Pekerjaan" => await _docGenerator.GeneratePermintaanPekerjaanAsync(
                    procurement
                ),
                "Service Order" => await _docGenerator.GenerateServiceOrderAsync(procurement),
                "Market Survey" => await _docGenerator.GenerateMarketSurveyAsync(procurement),
                "Surat Perintah Mulai Pekerjaan (SPMP)" => await _docGenerator.GenerateSPMPAsync(
                    procurement
                ),
                "Surat Penawaran Harga" => await _docGenerator.GenerateSuratPenawaranHargaAsync(
                    procurement
                ),
                "Surat Negosiasi Harga" => await _docGenerator.GenerateSuratNegosiasiHargaAsync(
                    procurement
                ),
                "Rencana Kerja dan Syarat-Syarat (RKS)" => await _docGenerator.GenerateRKSAsync(
                    procurement
                ),
                "Risk Assessment (RA)" => await _docGenerator.GenerateRiskAssessmentAsync(
                    procurement
                ),
                "Owner Estimate (OE)" => await _docGenerator.GenerateOwnerEstimateAsync(
                    procurement
                ),
                "Bill of Quantity (BOQ)" => await _docGenerator.GenerateBOQAsync(procurement),
                _ => throw new NotImplementedException(
                    $"Template untuk '{docType.Name}' belum tersedia"
                ),
            };

            return File(pdfBytes, "application/pdf", $"{docType.Name}_Preview.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ? NEW: GET /ProcurementDocuments/ApprovalTimeline/{procDocumentId}
    [HttpGet("ProcurementDocuments/ApprovalTimeline/{procDocumentId}")]
    public async Task<IActionResult> ApprovalTimeline(string procDocumentId)
    {
        if (string.IsNullOrWhiteSpace(procDocumentId))
            return BadRequest(new { ok = false, message = "Invalid procDocumentId." });

        try
        {
            var dto = await _approvalSvc.GetApprovalTimelineAsync(
                procDocumentId,
                HttpContext.RequestAborted
            );
            if (dto is null)
                return NotFound(new { ok = false, message = "Document was not found." });

            return Ok(new { ok = true, data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { ok = false, message = "Failed to load approval timeline." }
            );
        }
    }

    private static string SanitizeFileNameBase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "document";

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();

        foreach (var ch in name.Trim())
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }

        var result = sb.ToString().Replace(' ', '_').Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "document" : result;
    }
}
