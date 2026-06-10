using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.PR;

public partial class PurchaseRequisitionsController
{
    public async Task<ActionResult> Index(int page = 1, int pageSize = 10, string? search = null)
    {
        var fields = new HashSet<string> { "PrNumber", "Description" };
        var result = await _purchaseRequisitionQueryService.GetAllAsync(
            page,
            pageSize,
            search,
            fields
        );

        var viewModels = result
            .Items.Select(pr => new PurchaseRequisitionListViewModel
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                RequestDate = pr.RequestDate,
                Description = pr.Description,
                DocumentFileName = pr.DocumentFileName,
                ProcurementCount = pr.Procurements?.Count ?? 0,
                CreatedByUserId = pr.CreatedByUserId,
                CreatedByUserName = pr.CreatedByUser?.FullName,
                CreatedAt = pr.CreatedAt,
            })
            .ToList();

        return View(
            new Core.Common.PagedResult<PurchaseRequisitionListViewModel>
            {
                Items = viewModels,
                Page = result.Page,
                PageSize = result.PageSize,
                Total = result.Total,
            }
        );
    }

    public async Task<ActionResult> Details(string id)
    {
        var pr = await _purchaseRequisitionQueryService.GetByIdWithProcurementsAsync(id);
        if (pr == null)
            return NotFound();

        var viewModel = new PurchaseRequisitionDetailsViewModel
        {
            PrId = pr.PrId,
            PrNumber = pr.PrNumber,
            RequestDate = pr.RequestDate,
            Description = pr.Description,
            DocumentFileName = pr.DocumentFileName,
            DocumentFilePath = pr.DocumentFilePath,
            DocumentFileSize = pr.DocumentFileSize,
            CreatedByUserId = pr.CreatedByUserId,
            CreatedByUserName = pr.CreatedByUser?.FullName ?? pr.CreatedByUser?.UserName,
            CreatedAt = pr.CreatedAt,
            UpdatedAt = pr.UpdatedAt,
        };

        foreach (var procurement in pr.Procurements ?? [])
        {
            var requiredDocs = await _procurementDocumentQuery.GetRequiredDocsAsync(
                procurement.ProcurementId
            );
            var roundLetters = await _vendorRoundLetterRepository.ListByProcurementAsync(
                procurement.ProcurementId
            );

            var vendorNames =
                procurement
                    .ProfitLosses?.Where(pl => pl.SelectedVendor != null)
                    .Select(pl => pl.SelectedVendor!.VendorName)
                    .Distinct()
                    .ToList() ?? [];

            viewModel.Procurements.Add(
                new ProcurementWithDocsViewModel
                {
                    ProcurementId = procurement.ProcurementId,
                    ProcNum = procurement.ProcNum,
                    Wonum = procurement.Wonum,
                    JobName = procurement.JobName,
                    JobTypeName = procurement.JobType?.TypeName,
                    StatusName = procurement.Status?.StatusName,
                    Category = procurement.ProcurementCategory.ToString(),
                    StartDate = procurement.StartDate,
                    EndDate = procurement.EndDate,
                    VendorName = vendorNames.Any() ? string.Join(", ", vendorNames) : null,
                    RequiredDocuments = requiredDocs?.Items ?? [],
                    RoundLetters = roundLetters?.ToList() ?? [],
                    CompletedDocs = requiredDocs?.Items?.Count(d => d.Uploaded) ?? 0,
                    TotalDocs = requiredDocs?.Items?.Count ?? 0,
                }
            );
        }

        var procurementTrackings = new List<ProcurementTrackingDto>();
        foreach (var proc in viewModel.Procurements)
        {
            var tracking = await _procurementTrackingService.GetTrackingByProcurementIdAsync(
                proc.ProcurementId,
                HttpContext.RequestAborted
            );
            if (tracking != null)
                procurementTrackings.Add(tracking);
        }

        ViewData["ProcurementTrackings"] = procurementTrackings;
        return View(viewModel);
    }
}
