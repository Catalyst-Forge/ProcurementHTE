using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Web.Controllers.ApPoPickup
{
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
                procurements = await _queryService.GetMyAppoPickupsAsync(
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
                procurements = await _queryService.GetProcurementsForAppoApprovalAsync(
                    page,
                    pageSize,
                    search,
                    selectedFields,
                    ct
                );
            }

            if (tab == "mypickups")
                ViewData["PrStates"] = await LoadPrStatesAsync(procurements.Items, ct);

            ViewBag.CurrentTab = tab;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.Fields = fields;

            return View(procurements);
        }

        [Authorize(Roles = ActionRoles.ApPo)]
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

                await _workflowService.PickupAsync(id, userId);
                TempData["SuccessMessage"] = "Procurement berhasil di-pickup";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal pickup procurement: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index), new { tab = "mypickups" });
        }

        [Authorize(Roles = ActionRoles.ApPo)]
        [HttpPost("RaNumber/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRaNumber(string id, string raNumber, CancellationToken ct)
        {
            try
            {
                var procurement = await _queryService.GetProcurementByIdAsync(id);
                if (procurement == null)
                    return NotFound();

                if (!User.IsInRole("Admin") && procurement.AppoUserId != _userManager.GetUserId(User))
                {
                    TempData["ErrorMessage"] = "Nomor RA hanya bisa diisi oleh AP-PO yang pick up pekerjaan ini";
                    return RedirectToAction(nameof(Index), new { tab = "mypickups" });
                }

                await _workflowService.UpdateRaNumberAsync(id, raNumber);
                TempData["SuccessMessage"] = await RefreshRiskAssessmentAsync(id, ct)
                    ? "Nomor RA tersimpan dan dokumen Risk Assessment diperbarui"
                    : "Nomor RA tersimpan";
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { tab = "mypickups" });
        }

        [Authorize(Roles = ActionRoles.ApPo)]
        [HttpPost("SubmitInpro")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitInpro(
            string prId,
            string inproNumber,
            DateTime inproDate,
            DateTime inproSubmitDate,
            IFormFile? inproFile,
            CancellationToken ct
        )
        {
            var error = ValidatePdf(inproFile, "INPRO", maxMegabytes: 50);
            if (error == null && string.IsNullOrWhiteSpace(inproNumber))
                error = "Nomor INPRO wajib diisi.";
            if (error != null)
                return BackToMyPickups(false, error);

            var result = await _trackingService.SubmitInproAsync(
                prId,
                CurrentActor(),
                inproNumber.Trim(),
                inproDate,
                inproSubmitDate,
                ToUpload(inproFile!),
                ct
            );
            return BackToMyPickups(result.Success, result.Message);
        }

        [Authorize(Roles = ActionRoles.ApPo)]
        [HttpPost("SubmitHardcopy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitHardcopy(
            string prId,
            IFormFile? hardcopyFile,
            CancellationToken ct
        )
        {
            var error = ValidatePdf(hardcopyFile, "hardcopy evidence", maxMegabytes: 10);
            if (error != null)
                return BackToMyPickups(false, error);

            var result = await _trackingService.SubmitHardcopyAsync(
                prId,
                CurrentActor(),
                ToUpload(hardcopyFile!),
                ct
            );
            return BackToMyPickups(result.Success, result.Message);
        }

        [Authorize(Roles = ActionRoles.ApPo)]
        [HttpPost("SubmitPo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPo(string prId, string poNumber, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(poNumber))
                return BackToMyPickups(false, "Nomor PO wajib diisi.");

            var result = await _trackingService.SubmitPoAsync(prId, CurrentActor(), poNumber.Trim(), ct);
            return BackToMyPickups(result.Success, result.Message);
        }

        private async Task<Dictionary<string, PrSubmitState>> LoadPrStatesAsync(
            IEnumerable<Procurement> procurements,
            CancellationToken ct
        )
        {
            var states = new Dictionary<string, PrSubmitState>();
            foreach (var prId in procurements.Select(p => p.PrId).OfType<string>().Distinct())
            {
                var state = await _trackingService.GetPrSubmitStateAsync(prId, ct);
                if (state != null)
                    states[prId] = state;
            }

            return states;
        }

        private PrSubmitActor CurrentActor() =>
            new(_userManager.GetUserId(User) ?? string.Empty, User.IsInRole("Admin"));

        private static PrFileUpload ToUpload(IFormFile file) =>
            new(file.FileName, file.ContentType, file.Length, file.OpenReadStream());

        private static string? ValidatePdf(IFormFile? file, string label, int maxMegabytes)
        {
            if (file == null || file.Length == 0)
                return $"File {label} wajib diupload.";

            var isPdf =
                Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
            if (!isPdf)
                return $"Format file {label} harus PDF.";

            return file.Length > maxMegabytes * 1024L * 1024L
                ? $"Ukuran file {label} maksimal {maxMegabytes}MB."
                : null;
        }

        private RedirectToActionResult BackToMyPickups(bool success, string? message)
        {
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Index), new { tab = "mypickups" });
        }

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
