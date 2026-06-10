using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementDocumentsController
{
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
            var procurementEntity = await _queryService.GetProcurementByIdAsync(
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

            var generated = await _documentGenerator.GenerateAsync(
                docType.Name,
                procurementEntity,
                HttpContext.RequestAborted
            );
            if (!generated.Success)
            {
                TempData["ErrorMessage"] = generated.ErrorMessage;
                return RedirectToAction("Index", new { procurementId });
            }

            await _docSvc.SaveGeneratedAsync(
                new GeneratedProcDocumentRequest
                {
                    ProcurementId = procurementId,
                    DocumentTypeId = documentTypeId,
                    Bytes = generated.PdfBytes!,
                    FileName = $"{docType.Name}.pdf",
                    ContentType = "application/pdf",
                    Description = $"Generated from template on {DateTime.Now:dd MMM yyyy HH:mm}",
                    CreatedAt = DateTime.UtcNow,
                    GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    ProcDocumentId = procDocumentId,
                }
            );

            TempData["SuccessMessage"] = $"Dokumen '{docType.Name}' berhasil digenerate!";
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
            var procurement = await _queryService.GetProcurementByIdAsync(procurementId);
            if (procurement == null)
                return NotFound("Procurement was not found");

            var docType = await _docTypeRepo.GetByIdAsync(documentTypeId);
            if (docType == null)
                return NotFound("Document type was not found");

            var generated = await _documentGenerator.GenerateAsync(
                docType.Name,
                procurement,
                HttpContext.RequestAborted
            );
            if (!generated.Success)
                return BadRequest(new { error = generated.ErrorMessage });

            return File(generated.PdfBytes!, "application/pdf", $"{docType.Name}_Preview.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(
                new { error = $"Failed to preview generated document: {ex.Message}" }
            );
        }
    }
}
