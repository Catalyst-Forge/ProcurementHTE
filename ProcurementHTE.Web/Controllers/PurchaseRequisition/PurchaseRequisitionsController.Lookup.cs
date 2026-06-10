using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Web.Controllers.PR;

public partial class PurchaseRequisitionsController
{
    [HttpGet]
    public async Task<JsonResult> GetFilteredProcurements(
        string? vendorId = null,
        int? category = null,
        string? jobTypeId = null
    )
    {
        var procurements = await _procurementRepository.GetAllForSelectionAsync();
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        procurements = procurements
            .Where(p => p.PrId == null)
            .Where(p =>
                p.Status != null
                && string.Equals(
                    p.Status.StatusName,
                    "In Progress",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Where(p => !string.IsNullOrWhiteSpace(p.AppoUserId))
            .ToList();

        if (User.IsInRole("AP-PO") && !User.IsInRole("Admin"))
            procurements = procurements.Where(p => p.AppoUserId == currentUserId).ToList();

        if (!string.IsNullOrEmpty(vendorId))
        {
            procurements = procurements
                .Where(p =>
                    p.ProfitLosses != null
                    && p.ProfitLosses.Any(pl => pl.SelectedVendorId == vendorId)
                )
                .ToList();
        }

        if (category.HasValue)
            procurements = procurements
                .Where(p => p.ProcurementCategory == (ProcurementCategory)category.Value)
                .ToList();

        if (!string.IsNullOrEmpty(jobTypeId))
            procurements = procurements.Where(p => p.JobTypeId == jobTypeId).ToList();

        return Json(
            procurements.Select(p => new
            {
                id = p.ProcurementId,
                procNum = p.ProcNum,
                wonum = p.Wonum,
                jobName = p.JobName,
                category = p.ProcurementCategory.ToString(),
                categoryInt = (int)p.ProcurementCategory,
                jobType = p.JobType?.TypeName ?? "-",
                jobTypeId = p.JobTypeId,
                status = p.Status?.StatusName ?? "Created",
                startDate = p.StartDate.ToString("yyyy-MM-dd"),
                vendorId = p.ProfitLosses?.FirstOrDefault()?.SelectedVendorId ?? "",
                vendorName = p.ProfitLosses?.FirstOrDefault()?.SelectedVendor?.VendorName ?? "-",
            })
        );
    }

    [HttpGet]
    public async Task<JsonResult> GetLinkedProcurements(string prId)
    {
        if (string.IsNullOrEmpty(prId))
            return Json(Array.Empty<object>());

        var pr = await _purchaseRequisitionQueryService.GetByIdWithProcurementsAsync(prId);
        if (pr?.Procurements == null || !pr.Procurements.Any())
            return Json(Array.Empty<object>());

        return Json(
            pr.Procurements.Select(p => new
            {
                id = p.ProcurementId,
                procNum = p.ProcNum,
                wonum = p.Wonum,
                jobName = p.JobName,
                category = p.ProcurementCategory.ToString(),
                categoryInt = (int)p.ProcurementCategory,
                jobType = p.JobType?.TypeName ?? "-",
                jobTypeId = p.JobTypeId,
                status = p.Status?.StatusName ?? "Unknown",
                startDate = p.StartDate.ToString("yyyy-MM-dd"),
                vendorId = p.ProfitLosses?.FirstOrDefault()?.SelectedVendorId ?? "",
                vendorName = p.ProfitLosses?.FirstOrDefault()?.SelectedVendor?.VendorName ?? "-",
            })
        );
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendApproval(string procurementId, string prId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "User tidak teridentifikasi.";
            return RedirectToAction(nameof(Details), new { id = prId });
        }

        var result = await _procurementTrackingService.SendForApprovalAsync(procurementId, userId, ct);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = prId });
    }
}
