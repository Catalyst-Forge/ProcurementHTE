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

namespace ProcurementHTE.Web.Controllers.ApPoPickup
{
    /// <summary>
    /// The AP-PO table: every published job in one list, filled by clicking a cell. Nobody picks a
    /// job up first, so every AP-PO user works on every row.
    /// </summary>
    [Authorize]
    [Route("ApPoPickup")]
    public class ApPoPickupController : Controller
    {
        private const string ActivePageName = "AP-PO Pickup";
        private const string RiskAssessmentDocumentName = "Risk Assessment (RA)";
        private readonly IProcurementQueryService _queryService;
        private readonly IProcurementWorkflowService _workflowService;
        private readonly UserManager<User> _userManager;
        private readonly IProcDocumentService _procDocumentService;
        private readonly IProcurementDocumentGenerator _documentGenerator;
        private readonly IProcurementTrackingService _trackingService;

        public ApPoPickupController(
            IProcurementQueryService queryService,
            IProcurementWorkflowService workflowService,
            UserManager<User> userManager,
            IProcDocumentService procDocumentService,
            IProcurementDocumentGenerator documentGenerator,
            IProcurementTrackingService trackingService
        )
        {
            _queryService = queryService;
            _workflowService = workflowService;
            _userManager = userManager;
            _procDocumentService = procDocumentService;
            _documentGenerator = documentGenerator;
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

        [Authorize(Roles = ActionRoles.ApPo)]
        [HttpPost("Field/{id}")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> SaveField(string id, FinanceField field, string? value, CancellationToken ct) =>
            FinanceTableEndpoints.SaveFieldAsync(
                _trackingService,
                _queryService,
                FinanceTableKind.ApPo,
                id,
                field,
                value,
                _userManager.GetUserId(User),
                ct
            );

        [Authorize(Roles = ActionRoles.ApPo)]
        [HttpPost("SendApproval/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendApproval(string id, string? returnUrl, CancellationToken ct)
        {
            var result = await _trackingService.SendApprovalStepAsync(id, _userManager.GetUserId(User) ?? string.Empty, ct);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return BackToTable(returnUrl);
        }

        [Authorize(Roles = ActionRoles.ApPo)]
        [HttpPost("RaNumber/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRaNumber(string id, string raNumber, string? returnUrl, CancellationToken ct)
        {
            try
            {
                var procurement = await _queryService.GetProcurementByIdAsync(id);
                if (procurement == null)
                    return NotFound();

                await _workflowService.UpdateRaNumberAsync(id, raNumber);
                TempData["SuccessMessage"] = await RefreshRiskAssessmentAsync(id, ct)
                    ? "Nomor RA tersimpan dan dokumen Risk Assessment diperbarui"
                    : "Nomor RA tersimpan";
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return BackToTable(returnUrl);
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

                await PopulateUserFullNamesAsync(procurement);
                return View(procurement);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal memuat detail procurement: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // Back to the same page, filter, and sort the user acted from.
        private IActionResult BackToTable(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(Index));

        /// <summary>
        /// The Risk Assessment PDF is stored when it is generated, so a number
        /// added later only reaches the document by rendering it again. An
        /// uploaded copy is left alone.
        /// </summary>
        private async Task<bool> RefreshRiskAssessmentAsync(string procurementId, CancellationToken ct)
        {
            var documents = await _procDocumentService.ListByProcurementAsync(procurementId);
            var document = documents.FirstOrDefault(d =>
                d.DocumentType?.Name == RiskAssessmentDocumentName
                && d.Description?.StartsWith("Generated", StringComparison.Ordinal) == true
            );
            if (document == null)
                return false;

            var procurement = await _queryService.GetProcurementByIdAsync(procurementId);
            if (procurement == null)
                return false;

            var generated = await _documentGenerator.GenerateAsync(
                RiskAssessmentDocumentName,
                procurement,
                ct
            );
            if (!generated.Success)
            {
                TempData["WarningMessage"] =
                    $"Nomor RA tersimpan, tetapi dokumen gagal diperbarui: {generated.ErrorMessage}";
                return false;
            }

            await _procDocumentService.SaveGeneratedAsync(
                new GeneratedProcDocumentRequest
                {
                    ProcurementId = procurementId,
                    DocumentTypeId = document.DocumentTypeId,
                    Bytes = generated.PdfBytes,
                    FileName = document.FileName,
                    ContentType = "application/pdf",
                    Description = document.Description,
                    CreatedAt = DateTime.UtcNow,
                    GeneratedByUserId = _userManager.GetUserId(User),
                    ProcDocumentId = document.ProcDocumentId,
                },
                ct
            );
            return true;
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

        // Excel round trip: export the rows the page shows, then import an edited export back (preview first).
        [HttpGet("Export")]
        public Task<IActionResult> Export(string? search, [FromServices] IExcelExporter excel, CancellationToken ct) =>
            FinanceImportEndpoints.ExportAsync(_queryService, excel, Request, FinanceTableKind.ApPo, "AP Purchase Order", search, ct);

        [Authorize(Roles = ActionRoles.ApPo)]
        [HttpGet("Import")]
        public IActionResult Import() => FinanceImportEndpoints.Page(this, FinanceImportTarget.ApPo);

        [Authorize(Roles = ActionRoles.ApPo)]
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
        ) => FinanceImportEndpoints.PreviewAsync(this, FinanceImportTarget.ApPo, file, reader, imports, cache, ct);

        [Authorize(Roles = ActionRoles.ApPo)]
        [HttpPost("ImportApply")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ImportApply(
            string? token,
            [FromServices] IFinanceImportService imports,
            [FromServices] IMemoryCache cache,
            CancellationToken ct
        ) => FinanceImportEndpoints.ApplyAsync(this, FinanceImportTarget.ApPo, token, imports, cache, ct);
    }
}
