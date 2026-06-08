using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementDocumentsController
{
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

            var procurement = await _queryService.GetProcurementByIdAsync(procurementId);
            ViewBag.ProcNum = procurement?.ProcNum ?? "-";
            ViewBag.Vendors = await _vendorService.GetAllVendorsAsync();
            ViewBag.RoundLetters = await _roundLetterRepo.ListByProcurementAsync(procurementId);

            var filteredDocItems = (dto.Items ?? Enumerable.Empty<RequiredDocItemDto>()).Where(
                item =>
                {
                    var docName = item.DocumentTypeName?.Trim();
                    return string.IsNullOrWhiteSpace(docName)
                        || !_roundLetterDocNames.Contains(docName);
                }
            );

            var vm = new ProcurementRequiredDocsVm
            {
                ProcurementId = dto.ProcurementId,
                JobTypeId = dto.JobTypeId,
                Items =
                [
                    .. filteredDocItems.Select(x => new RequiredDocItemDto
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
                    }),
                ],
            };

            return View(vm);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to load document list: {ex.Message}";
            return RedirectToAction("Index", "Error");
        }
    }
}
