using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Web.Controllers.AppoApproval
{
    [Authorize(Roles = "Admin, AP-PO")]
    [Route("ProcurementOrder")]
    public class ProcurementOrderController : Controller
    {
        private const string ActivePageName = "Procurement Order";
        private readonly IProcurementService _procurementService;
        private readonly UserManager<User> _userManager;

        public ProcurementOrderController(
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
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? fields = null,
            CancellationToken ct = default
        )
        {
            var allowed = new[] { 10, 25, 50, 100 };
            if (!allowed.Contains(pageSize))
                pageSize = 10;

            var selectedFields = (fields ?? "ProcNum, JobName")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var procurements = await _procurementService.GetProcurementsForAppoApprovalAsync(
                page,
                pageSize,
                search,
                selectedFields,
                ct
            );

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.Fields = fields;

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

                await _procurementService.PickupAsync(id, userId);
                TempData["SuccessMessage"] = "Procurement berhasil di-pickup";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal pickup procurement: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
