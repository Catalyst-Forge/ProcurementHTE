using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.ApInvoicePickup
{
    [Authorize(Roles = "Admin, AP-Invoice")]
    [Route("ApInvoicePickup")]
    public class ApInvoicePickupController : Controller
    {
        private const string ActivePageName = "AP-Invoice Pickup";
        private readonly IProcurementQueryService _queryService;
        private readonly IProcurementWorkflowService _workflowService;
        private readonly UserManager<User> _userManager;

        public ApInvoicePickupController(
            IProcurementQueryService queryService,
            IProcurementWorkflowService workflowService,
            UserManager<User> userManager
        )
        {
            _queryService = queryService;
            _workflowService = workflowService;
            _userManager = userManager;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActivePage = ActivePageName;
            base.OnActionExecuting(context);
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            string tab = "waiting",
            int page = 1,
            int pageSize = 10,
            string? search = null,
            CancellationToken ct = default
        )
        {
            var allowed = new[] { 10, 25, 50, 100 };
            if (!allowed.Contains(pageSize))
                pageSize = 10;

            var userId = _userManager.GetUserId(User);

            // Determine which data to fetch based on tab
            Core.Common.PagedResult<Procurement> procurements;
            if (tab == "mypickups" && !string.IsNullOrEmpty(userId))
            {
                procurements = await _queryService.GetMyApInvoicePickupsAsync(
                    userId,
                    page,
                    pageSize,
                    search,
                    ct
                );
            }
            else
            {
                // Default: waiting for pickup (pending filter)
                procurements = await _queryService.GetProcurementsForApInvoiceAsync(
                    page,
                    pageSize,
                    search,
                    "pending",
                    ct
                );
            }

            ViewBag.CurrentTab = tab;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;

            return View(procurements);
        }

        [HttpPost("Pickup/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pickup(string id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                await _workflowService.PickupForApInvoiceAsync(id, userId);
                TempData["SuccessMessage"] = "Procurement berhasil di-pickup untuk AP-Invoice";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal pickup procurement: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var procurement = await _queryService.GetProcurementByIdAsync(id);
                if (procurement == null)
                    return NotFound();

                return View(procurement);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal memuat detail procurement: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("UpdateInvoiceData/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInvoiceData(
            string id,
            string? saNo,
            string? sp3No
        )
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                await _workflowService.UpdateInvoiceDataAsync(id, saNo, sp3No, userId);
                TempData["SuccessMessage"] = "Data invoice berhasil diperbarui";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal memperbarui data invoice: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
