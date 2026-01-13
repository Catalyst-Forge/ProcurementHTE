using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Web.Controllers.ApPoPickup
{
    [Authorize(Roles = "Admin, AP-PO")]
    [Route("ApPoPickup")]
    public class ApPoPickupController : Controller
    {
        private const string ActivePageName = "AP-PO Pickup";
        private readonly IProcurementService _procurementService;
        private readonly UserManager<User> _userManager;

        public ApPoPickupController(
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

            var userId = _userManager.GetUserId(User);

            // Determine which data to fetch based on tab
            Core.Common.PagedResult<Procurement> procurements;
            if (tab == "mypickups" && !string.IsNullOrEmpty(userId))
            {
                procurements = await _procurementService.GetMyAppoPickupsAsync(
                    userId,
                    page,
                    pageSize,
                    search,
                    selectedFields,
                    ct
                );
            }
            else
            {
                procurements = await _procurementService.GetProcurementsForAppoApprovalAsync(
                    page,
                    pageSize,
                    search,
                    selectedFields,
                    ct
                );
            }

            ViewBag.CurrentTab = tab;
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

                // Populate user full names for approvers
                await PopulateUserFullNamesAsync(procurement);

                return View(procurement);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal memuat detail procurement: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PopulateUserFullNamesAsync(Procurement procurement)
        {
            var userIds = new[]
            {
                procurement.PicOpsUserId,
                procurement.AnalystHteUserId,
                procurement.AssistantManagerUserId,
                procurement.ManagerUserId,
            }.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            var users = new Dictionary<string, string>();
            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId!);
                if (user != null)
                {
                    users[userId!] = user.FullName ?? user.UserName ?? userId!;
                }
            }

            string Resolve(string? id) => !string.IsNullOrEmpty(id) && users.TryGetValue(id, out var name) ? name : "-";

            ViewBag.PicOpsName = Resolve(procurement.PicOpsUserId);
            ViewBag.AnalystName = Resolve(procurement.AnalystHteUserId);
            ViewBag.AssistantManagerName = Resolve(procurement.AssistantManagerUserId);
            ViewBag.ManagerName = Resolve(procurement.ManagerUserId);
        }
    }
}
