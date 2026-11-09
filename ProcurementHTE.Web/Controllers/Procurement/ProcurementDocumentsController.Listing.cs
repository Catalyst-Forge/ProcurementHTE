using ProcurementHTE.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementDocumentsController
{
    [Authorize(Roles = ActionRoles.ApPo)]
    [HttpGet("ProcurementDocuments/Index/{procurementId}")]
    // AP-PO opens this page from a job's name (from=appo): the job's detail and tracking then
    // lead the page, above the SPH, SNH, and documents it already shows.
    public async Task<IActionResult> Index(string procurementId, string? from = null)
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
            ViewBag.ProcNum = procurement?.DisplayId ?? "-";
            ViewBag.Vendors = await _vendorQueryService.GetAllVendorsAsync();
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

            if (string.Equals(from, JobDetailOrigin.ApPo.Key, StringComparison.OrdinalIgnoreCase)
                && await _ldp.GetByProcurementIdAsync(procurementId) is { } job)
            {
                vm.Detail = new JobDetailViewModel
                {
                    Job = job,
                    Tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId),
                    Origin = JobDetailOrigin.ApPo,
                };
            }

            return View(vm);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to load document list: {ex.Message}";
            return RedirectToAction("Index", "Error");
        }
    }
}
