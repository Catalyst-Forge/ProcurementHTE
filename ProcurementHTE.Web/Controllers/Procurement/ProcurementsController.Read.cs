using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Web.Mappers;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    [Authorize(Policy = Permissions.Procurement.Read)]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? fields = null,
        CancellationToken ct = default,
        string? userId = null
    )
    {
        var allowed = new[] { 10, 25, 50, 100 };
        if (!allowed.Contains(pageSize))
            pageSize = 10;

        var selectedFields = (fields ?? "ProcNum, JobName")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var user = _userManager.GetUserId(User);
        var procurements = await _queryService.GetAllProcurementWithDetailsAsync(
            page,
            pageSize,
            search,
            selectedFields,
            ct,
            user
        );

        ViewBag.RouteData = new RouteValueDictionary
        {
            ["ActivePage"] = ActivePageName,
            ["search"] = search,
            ["fields"] = string.Join(',', selectedFields),
            ["pageSize"] = pageSize,
        };
        ViewBag.UserNames = await BuildUserNameMapAsync(procurements.Items);

        return View(procurements);
    }

    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        try
        {
            var procurement = await _queryService.GetProcurementByIdAsync(id);
            if (procurement == null)
                return NotFound();

            await PopulateUserFullNamesAsync(procurement);

            var summary = await _pnlQueryService.GetSummaryByProcurementAsync(id);
            if (summary != null)
            {
                ViewBag.SelectedVendorNames = summary.SelectedVendorNames;
                ViewBag.PnlViewModel = ProfitLossViewModelMapper.ToSummaryViewModel(summary);
            }

            var documents = await _procDocService.ListByProcurementAsync(id);
            if (documents != null)
                ViewBag.Documents = documents;

            return View(procurement);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Failed to load procurement details: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Procurement.Create)]
    public IActionResult RenderRaNumberField(ProcurementCategory category, string? raNumber)
    {
        if (category != ProcurementCategory.Jasa)
            return Content(string.Empty);

        ViewBag.RaNumberValue = raNumber;
        return PartialView("_RaNumberField");
    }
}
