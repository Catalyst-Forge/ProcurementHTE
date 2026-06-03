using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.ArPickup
{
    [Authorize(Roles = "Admin, AR")]
    [Route("ArPickup")]
    public class ArPickupController : Controller
    {
        private const string ActivePageName = "AR Pickup";
        private readonly IProcurementService _procurementService;
        private readonly UserManager<User> _userManager;

        public ArPickupController(
            IProcurementService procurementService,
            UserManager<User> userManager
        )
        {
            _procurementService = procurementService;
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

            Core.Common.PagedResult<Procurement> procurements;
            if (tab == "mypickups" && !string.IsNullOrEmpty(userId))
            {
                procurements = await _procurementService.GetMyArPickupsAsync(
                    userId,
                    page,
                    pageSize,
                    search,
                    ct
                );
            }
            else
            {
                // Default: show pending (waiting to be picked up)
                procurements = await _procurementService.GetProcurementsForArPickupAsync(
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

                await _procurementService.PickupForArAsync(id, userId);
                TempData["SuccessMessage"] = "Procurement berhasil di-pickup untuk AR. Silakan isi data accrual.";

                // Redirect to Accrual page to fill in accrual data
                return RedirectToAction("Index", "Accrual");
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
                var procurement = await _procurementService.GetProcurementByIdAsync(id);
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
    }
}
