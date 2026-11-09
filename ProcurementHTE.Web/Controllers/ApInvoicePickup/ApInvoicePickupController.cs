using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Authorization;
using ProcurementHTE.Web.Controllers.Finance;

namespace ProcurementHTE.Web.Controllers.ApInvoicePickup
{
    [Authorize]
    [Route("ApInvoicePickup")]
    public class ApInvoicePickupController : Controller
    {
        private const string ActivePageName = "AP-Invoice Pickup";
        private readonly IProcurementQueryService _queryService;
        private readonly IProcurementWorkflowService _workflowService;
        private readonly UserManager<User> _userManager;
        private readonly IProcurementTrackingService _trackingService;

        public ApInvoicePickupController(
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

        // SA No, SP3 No, and the PIC fill in without waiting for AP-PO's PO number.
        [Authorize(Roles = ActionRoles.ApInvoice)]
        [HttpPost("Field/{id}")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> SaveField(string id, FinanceField field, string? value, CancellationToken ct) =>
            FinanceTableEndpoints.SaveFieldAsync(
                _trackingService,
                _queryService,
                FinanceTableKind.ApInvoice,
                id,
                field,
                value,
                _userManager.GetUserId(User),
                ct
            );

        /// <summary>
        /// The old invoice detail page. The invoice is filled in the AP-Invoice table now, so the
        /// job's detail page, shared with LDP and Data Accrual, takes its place.
        /// </summary>
        [HttpGet("Details/{id}")]
        public IActionResult Details(string id) =>
            string.IsNullOrEmpty(id)
                ? NotFound()
                : RedirectToAction("Details", "Ldp", new { id, from = "apinvoice" });

        // Excel round trip: export the rows the page shows, then import an edited export back (preview first).
        [HttpGet("Export")]
        public Task<IActionResult> Export(string? search, [FromServices] IExcelExporter excel, CancellationToken ct) =>
            FinanceImportEndpoints.ExportAsync(_queryService, excel, Request, FinanceTableKind.ApInvoice, "AP Invoice", search, ct);

        [Authorize(Roles = ActionRoles.ApInvoice)]
        [HttpGet("Import")]
        public IActionResult Import() => FinanceImportEndpoints.Page(this, FinanceImportTarget.ApInvoice);

        [Authorize(Roles = ActionRoles.ApInvoice)]
        [HttpPost("ImportPreview")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(FinanceImportEndpoints.MaxBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = FinanceImportEndpoints.MaxBytes)]
        public Task<IActionResult> ImportPreview(
            IFormFile? file,
            [FromServices] IExcelTableReader reader,
            [FromServices] IFinanceImportService imports,
            [FromServices] IMemoryCache cache,
            CancellationToken ct
        ) => FinanceImportEndpoints.PreviewAsync(this, FinanceImportTarget.ApInvoice, file, reader, imports, cache, ct);

        [Authorize(Roles = ActionRoles.ApInvoice)]
        [HttpPost("ImportApply")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ImportApply(
            string? token,
            [FromServices] IFinanceImportService imports,
            [FromServices] IMemoryCache cache,
            CancellationToken ct
        ) => FinanceImportEndpoints.ApplyAsync(this, FinanceImportTarget.ApInvoice, token, imports, cache, ct);
    }
}
