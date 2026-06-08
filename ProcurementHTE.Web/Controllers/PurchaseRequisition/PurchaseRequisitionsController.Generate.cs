using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.PR;

public partial class PurchaseRequisitionsController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateDocument(
        string prId,
        string procurementId,
        string documentTypeId,
        string? procDocumentId
    )
    {
        try
        {
            var procurement = await _queryService.GetProcurementByIdAsync(procurementId);
            if (procurement == null)
            {
                TempData["ErrorMessage"] = "Procurement not found.";
                return RedirectToAction(nameof(Details), new { id = prId });
            }

            var docType = await _documentTypeRepository.GetByIdAsync(documentTypeId);
            if (docType == null)
            {
                TempData["ErrorMessage"] = "Document type not found.";
                return RedirectToAction(nameof(Details), new { id = prId });
            }

            var generated = await _documentGenerator.GenerateAsync(
                docType.Name,
                procurement,
                HttpContext.RequestAborted
            );
            if (!generated.Success)
            {
                TempData["ErrorMessage"] = generated.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id = prId });
            }

            await SaveGeneratedDocumentAsync(procurementId, documentTypeId, procDocumentId, docType.Name, generated.PdfBytes!);
            TempData["SuccessMessage"] = $"Document '{docType.Name}' generated successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to generate document: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id = prId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateDocumentAjax(
        string prId,
        string procurementId,
        string documentTypeId,
        string? procDocumentId
    )
    {
        try
        {
            var procurement = await _queryService.GetProcurementByIdAsync(procurementId);
            if (procurement == null)
                return Json(new { success = false, message = "Procurement not found." });

            var docType = await _documentTypeRepository.GetByIdAsync(documentTypeId);
            if (docType == null)
                return Json(new { success = false, message = "Document type not found." });

            var generated = await _documentGenerator.GenerateAsync(
                docType.Name,
                procurement,
                HttpContext.RequestAborted
            );
            if (!generated.Success)
                return Json(new { success = false, message = generated.ErrorMessage });

            var result = await SaveGeneratedDocumentAsync(
                procurementId,
                documentTypeId,
                procDocumentId,
                docType.Name,
                generated.PdfBytes!
            );

            return Json(
                new
                {
                    success = true,
                    message = $"Document '{docType.Name}' generated successfully!",
                    procDocumentId = result.ProcDocumentId,
                    fileName = result.FileName,
                    documentTypeName = docType.Name,
                }
            );
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Failed to generate document: {ex.Message}" });
        }
    }

    private async Task<UploadProcDocumentResult> SaveGeneratedDocumentAsync(
        string procurementId,
        string documentTypeId,
        string? procDocumentId,
        string documentTypeName,
        byte[] pdfBytes
    )
    {
        return await _procDocumentService.SaveGeneratedAsync(
            new GeneratedProcDocumentRequest
            {
                ProcurementId = procurementId,
                DocumentTypeId = documentTypeId,
                Bytes = pdfBytes,
                FileName = $"{documentTypeName}.pdf",
                ContentType = "application/pdf",
                Description = $"Generated from PR Service on {DateTime.Now:dd MMM yyyy HH:mm}",
                CreatedAt = DateTime.UtcNow,
                GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                ProcDocumentId = procDocumentId,
            }
        );
    }
}
