using ProcurementHTE.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Controllers.Finance;

namespace ProcurementHTE.Web.Controllers.ArPickup
{
    [Authorize]
    [Route("ArPickup")]
    public class ArPickupController : Controller
    {
        private const string ActivePageName = "AR Pickup";
        private readonly IProcurementQueryService _queryService;
        private readonly IProcurementWorkflowService _workflowService;
        private readonly UserManager<User> _userManager;
        private readonly IProcurementTrackingService _trackingService;

        public ArPickupController(
            IProcurementQueryService queryService,
            IProcurementWorkflowService workflowService,
            UserManager<User> userManager,
            IProcurementTrackingService trackingService
        )
        {
            _queryService = queryService;
            _workflowService = workflowService;
            _userManager = userManager;
            _trackingService = trackingService;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActivePage = ActivePageName;
            base.OnActionExecuting(context);
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 25,
            string? search = null,
            CancellationToken ct = default
        )
        {
            ViewBag.Search = search;
            return View(await FinanceTableEndpoints.LoadAsync(_queryService, Request, search, page, pageSize, ct));
        }

        [Authorize(Roles = ActionRoles.Ar)]
        [HttpPost("Field/{id}")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> SaveField(string id, FinanceField field, string? value, CancellationToken ct) =>
            FinanceTableEndpoints.SaveFieldAsync(
                _trackingService,
                _queryService,
                FinanceTableKind.Ar,
                id,
                field,
                value,
                _userManager.GetUserId(User),
                ct
            );

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
    }
}
