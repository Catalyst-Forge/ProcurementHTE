using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule
{
    [Authorize]
    public class ProcurementsController : Controller
    {
        #region Construct

        private const string ActivePageName = "Index Procurements";
        private static readonly Regex LetterFileFieldRegex = new(
            "^Vendors\\[(\\d+)\\]\\.LetterFiles\\[(\\d+)\\]$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
        );
        private readonly IProcurementService _procurementService;
        private readonly IVendorService _vendorService;
        private readonly IProfitLossService _pnlService;
        private readonly IVendorOfferService _voService;
        private readonly IDocumentGenerator _documentGenerator;
        private readonly IDocumentTypeService _docTypeService;
        private readonly IProcDocumentService _procDocService;
        private readonly IVendorRoundLetterRepository _roundLetterRepository;
        private readonly UserManager<User> _userManager;
        private readonly IUnitTypeRepository _unitTypeRepository;
        private readonly INotificationService _notificationService;
        private readonly IProcurementTrackingService _trackingService;
        private readonly IQrCodeGenerator _qrCodeGenerator;

        public ProcurementsController(
            IProcurementService procurementService,
            IVendorService vendorService,
            IProfitLossService pnlService,
            IVendorOfferService voService,
            IDocumentGenerator documentGenerator,
            IDocumentTypeService docTypeService,
            IProcDocumentService procDocService,
            IVendorRoundLetterRepository roundLetterRepository,
            UserManager<User> userManager,
            IUnitTypeRepository unitTypeRepository,
            INotificationService notificationService,
            IProcurementTrackingService trackingService,
            IQrCodeGenerator qrCodeGenerator
        )
        {
            _procurementService = procurementService;
            _vendorService = vendorService;
            _pnlService = pnlService;
            _voService = voService;
            _documentGenerator = documentGenerator;
            _docTypeService = docTypeService;
            _procDocService = procDocService;
            _roundLetterRepository = roundLetterRepository;
            _userManager = userManager;
            _unitTypeRepository = unitTypeRepository;
            _notificationService = notificationService;
            _trackingService = trackingService;
            _qrCodeGenerator = qrCodeGenerator;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActivePage = ActivePageName;
            base.OnActionExecuting(context);
        }

        #endregion

        #region CRUD Operations

        // GET: Procurements
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

            var procurements = await _procurementService.GetAllProcurementWithDetailsAsync(
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

        // GET: Procurements/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var procurement = await _procurementService.GetProcurementByIdAsync(id);
                if (procurement == null)
                    return NotFound();

                await PopulateUserFullNamesAsync(procurement);

                var summary = await _pnlService.GetSummaryByProcurementAsync(id);
                if (summary != null)
                {
                    var viewModel = MapToViewModel(summary);
                    ViewBag.SelectedVendorNames = summary.SelectedVendorNames;
                    ViewBag.PnlViewModel = viewModel;
                }

                var documents = await _procDocService.ListByProcurementAsync(id);
                if (documents != null)
                {
                    ViewBag.Documents = documents;
                }

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

        // GET: Procurements/Create
        [HttpGet]
        [Authorize(Policy = Permissions.Procurement.Create)]
        public async Task<IActionResult> Create(
            string? jobTypeId = null,
            ProcurementCategory? category = null
        )
        {
            var viewModel = await BuildCreateViewModelAsync(jobTypeId, category);
            return View("CreateByType", viewModel);
        }

        // POST: Procurements/Create
        // Post form Procurement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Procurement.Create)]
        public async Task<IActionResult> Create(
            [FromForm] ProcurementCreateViewModel procurementViewModel,
            string submitAction
        )
        {
            Console.WriteLine($"=== DEBUG: Create POST received ===");
            Console.WriteLine($"DEBUG: submitAction = '{submitAction}'");
            
            // Remove auto-generated validation errors (we handle validation manually)
            RemoveAutoGeneratedProcurementValidation();
            RemoveDetailsValidation();
            RemoveOffersValidation();
            
            Console.WriteLine($"DEBUG: ModelState.IsValid after removing auto-validation = {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys.Where(k => ModelState[k]?.Errors.Count > 0))
                {
                    Console.WriteLine($"DEBUG: ModelState error key='{key}', errors={string.Join("; ", ModelState[key]?.Errors.Select(e => e.ErrorMessage) ?? Enumerable.Empty<string>())}");
                }
            }

            // Initialize defaults
            procurementViewModel.Procurement ??= new Procurement();
            procurementViewModel.Details ??= new List<ProcDetail>();
            procurementViewModel.Offers ??= new List<ProcOffer>();

            // Set current user as creator and PIC
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("User tidak terautentikasi");
            
            var proc = procurementViewModel.Procurement;
            proc.UserId = currentUserId;
            proc.PicOpsUserId = currentUserId;

            // Determine if this is a draft save
            bool isDraft = submitAction?.Equals("Draft", StringComparison.OrdinalIgnoreCase) == true;
            Console.WriteLine($"DEBUG: isDraft = {isDraft}");

            // === STEP 1: Validate and set Status ===
            var status = await _procurementService.GetStatusByNameAsync(submitAction ?? "");
            if (status == null)
            {
                Console.WriteLine($"DEBUG: Status '{submitAction}' not found!");
                ModelState.AddModelError("", $"Status '{submitAction}' tidak ditemukan di database. Pastikan ada entry 'Draft' dan 'Created' di tabel Statuses.");
                await RepopulateCreateViewModel(procurementViewModel);
                return View("CreateByType", procurementViewModel);
            }
            proc.StatusId = status.StatusId;
            Console.WriteLine($"DEBUG: StatusId set to = {proc.StatusId}");

            // === STEP 2: Run validations based on mode ===
            ValidateProcurementForm(procurementViewModel, isDraft);
            Console.WriteLine($"DEBUG: After ValidateProcurementForm, ModelState.IsValid = {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys.Where(k => ModelState[k]?.Errors.Count > 0))
                {
                    Console.WriteLine($"DEBUG: Validation error key='{key}', errors={string.Join("; ", ModelState[key]?.Errors.Select(e => e.ErrorMessage) ?? Enumerable.Empty<string>())}");
                }
            }

            // === STEP 3: Check ModelState and return if invalid ===
            if (!ModelState.IsValid)
            {
                Console.WriteLine("DEBUG: Returning view due to validation errors");
                await RepopulateCreateViewModel(procurementViewModel);
                return View("CreateByType", procurementViewModel);
            }

            Console.WriteLine("DEBUG: Validation passed, proceeding to save...");

            // === STEP 4: Set default values for required DB fields ===
            // Only after validation passes, set defaults for fields not filled in form
            if (string.IsNullOrWhiteSpace(proc.AnalystHteUserId))
                proc.AnalystHteUserId = currentUserId;
            if (string.IsNullOrWhiteSpace(proc.AssistantManagerUserId))
                proc.AssistantManagerUserId = currentUserId;
            if (string.IsNullOrWhiteSpace(proc.ManagerUserId))
                proc.ManagerUserId = currentUserId;
            if (string.IsNullOrWhiteSpace(proc.JobName))
                proc.JobName = isDraft ? "(Draft)" : proc.JobName;

            // === STEP 5: Save to database ===
            try
            {
                Console.WriteLine("=== DEBUG: STEP 5 - Saving to database ===");
                Console.WriteLine($"DEBUG: JobTypeId = {proc.JobTypeId}");
                Console.WriteLine($"DEBUG: StatusId = {proc.StatusId}");
                Console.WriteLine($"DEBUG: JobName = {proc.JobName}");
                Console.WriteLine($"DEBUG: UserId = {proc.UserId}");
                Console.WriteLine($"DEBUG: PicOpsUserId = {proc.PicOpsUserId}");
                Console.WriteLine($"DEBUG: AnalystHteUserId = {proc.AnalystHteUserId}");
                Console.WriteLine($"DEBUG: Offers count = {procurementViewModel.Offers?.Count ?? 0}");
                Console.WriteLine($"DEBUG: Details count = {procurementViewModel.Details?.Count ?? 0}");

                // Append suffix to SpmpNumber and OeNumber
                proc.SpmpNumber = AppendSuffixIfNeeded(proc.SpmpNumber);
                proc.OeNumber = AppendSuffixIfNeeded(proc.OeNumber);

                await _procurementService.AddProcurementWithDetailsAsync(
                    procurementViewModel.Procurement,
                    procurementViewModel.Details ?? new List<ProcDetail>(),
                    procurementViewModel.Offers ?? new List<ProcOffer>()
                );

                Console.WriteLine("=== DEBUG: Successfully saved to database ===");

                TempData["SuccessMessage"] = isDraft
                    ? "Procurement saved as draft successfully"
                    : "Procurement created successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"=== DEBUG ERROR (ArgumentException): {ex.Message} ===");
                ModelState.AddModelError("", $"Validasi gagal: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"=== DEBUG ERROR (DbUpdateException): {ex.InnerException?.Message ?? ex.Message} ===");
                ModelState.AddModelError("", $"Gagal menyimpan data ke database: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"=== DEBUG ERROR (InvalidOperationException): {ex.Message} ===");
                ModelState.AddModelError("", $"Operasi tidak valid: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== DEBUG ERROR (Exception): {ex.Message} ===");
                Console.WriteLine($"=== DEBUG ERROR Stack: {ex.StackTrace} ===");
                ModelState.AddModelError("", $"Terjadi kesalahan saat menyimpan data: {ex.Message}");
            }

            await RepopulateCreateViewModel(procurementViewModel);
            return View("CreateByType", procurementViewModel);
        }

        /// <summary>
        /// Validates the procurement form based on whether it's a draft or full create.
        /// Draft mode: Only JobType is required
        /// Create mode: All required fields are validated
        /// </summary>
        private void ValidateProcurementForm(ProcurementCreateViewModel vm, bool isDraft)
        {
            var proc = vm.Procurement;
            var prefix = $"{nameof(vm.Procurement)}";

            // === Always required (even for Draft) ===
            if (string.IsNullOrWhiteSpace(proc.JobTypeId))
            {
                ModelState.AddModelError($"{prefix}.{nameof(proc.JobTypeId)}", "Pilih job type terlebih dahulu");
            }

            // === Skip remaining validations for Draft ===
            if (isDraft) return;

            // === Required for Create only ===
            if (proc.ContractType == 0)
            {
                ModelState.AddModelError($"{prefix}.{nameof(proc.ContractType)}", "Contract type wajib dipilih");
            }

            if (proc.ProcurementCategory == 0)
            {
                ModelState.AddModelError($"{prefix}.{nameof(proc.ProcurementCategory)}", "Jenis pengadaan wajib dipilih");
            }

            if (string.IsNullOrWhiteSpace(proc.JobName))
            {
                ModelState.AddModelError($"{prefix}.{nameof(proc.JobName)}", "Nama pekerjaan wajib diisi");
            }

            if (proc.DocumentDate == default)
            {
                ModelState.AddModelError($"{prefix}.{nameof(proc.DocumentDate)}", "Tanggal dokumen wajib diisi");
            }

            if (proc.StartDate == default)
            {
                ModelState.AddModelError($"{prefix}.{nameof(proc.StartDate)}", "Tanggal mulai wajib diisi");
            }

            if (proc.EndDate == default)
            {
                ModelState.AddModelError($"{prefix}.{nameof(proc.EndDate)}", "Tanggal selesai wajib diisi");
            }

            // Date range validation
            if (proc.StartDate != default && proc.EndDate != default && proc.EndDate < proc.StartDate)
            {
                ModelState.AddModelError($"{prefix}.{nameof(proc.EndDate)}", "Tanggal selesai harus setelah tanggal mulai");
            }

            // RA Number required for Jasa
            if (proc.ProcurementCategory == ProcurementCategory.Jasa && string.IsNullOrWhiteSpace(proc.RaNumber))
            {
                ModelState.AddModelError($"{prefix}.{nameof(proc.RaNumber)}", "RA Number wajib diisi untuk jenis pengadaan jasa");
            }

            // === Offer Items validation (Create only) ===
            if (vm.Offers == null || vm.Offers.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.Offers), "Minimal harus ada 1 item penawaran untuk membuat procurement");
                return;
            }

            // Validate each offer item
            var offerIndexes = Request.Form["Offers.Index"].ToArray();
            for (int i = 0; i < offerIndexes.Length; i++)
            {
                var idx = offerIndexes[i];
                var offer = vm.Offers.ElementAtOrDefault(i);
                if (offer == null) continue;

                if (string.IsNullOrWhiteSpace(offer.ItemPenawaran))
                {
                    ModelState.AddModelError($"Offers[{idx}].ItemPenawaran", "Item penawaran wajib diisi");
                }
                if (offer.Qty <= 0)
                {
                    ModelState.AddModelError($"Offers[{idx}].Qty", "Qty harus lebih dari 0");
                }
                if (string.IsNullOrWhiteSpace(offer.Unit))
                {
                    ModelState.AddModelError($"Offers[{idx}].Unit", "Unit wajib diisi");
                }
            }
        }

        // GET: Procurements/Edit/5
        [Authorize(Policy = Permissions.Procurement.Edit)]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var procurement = await _procurementService.GetProcurementByIdAsync(id);
            if (procurement == null)
                return NotFound();

            // Check status-based authorization
            if (!CanUserEditProcurementByStatus(procurement))
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit procurement dengan status ini.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var (jobTypes, statuses) =
                    await _procurementService.GetRelatedEntitiesForProcurementAsync();

                var viewModel = new ProcurementEditViewModel
                {
                    ProcurementId = procurement.ProcurementId,
                    ProcNum = procurement.ProcNum ?? string.Empty,
                    JobTypeId = procurement.JobTypeId,
                    ContractType = procurement.ContractType,
                    ProcurementCategory = procurement.ProcurementCategory,
                    JobName = procurement.JobName,
                    SpkNumber = procurement.SpkNumber,
                    DocumentDate = procurement.DocumentDate,
                    StartDate = procurement.StartDate,
                    EndDate = procurement.EndDate,
                    ProjectRegion = procurement.ProjectRegion,
                    PotentialAccrualDate = procurement.PotentialAccrualDate,
                    SpmpNumber = RemoveSuffixIfNeeded(procurement.SpmpNumber), // Hapus suffix untuk edit
                    MemoNumber = procurement.MemoNumber,
                    OeNumber = RemoveSuffixIfNeeded(procurement.OeNumber), // Hapus suffix untuk edit
                    RaNumber = procurement.RaNumber,
                    NoRig = procurement.NoRig,
                    NoHte = procurement.NoHte,
                    ProjectCode = procurement.ProjectCode,
                    Wonum = procurement.Wonum,
                    LtcName = procurement.LtcName,
                    Note = procurement.Note,
                    StatusId = procurement.StatusId,
                    PicOpsUserId = procurement.PicOpsUserId,
                    AnalystHteUserId = procurement.AnalystHteUserId,
                    AssistantManagerUserId = procurement.AssistantManagerUserId,
                    ManagerUserId = procurement.ManagerUserId,
                    Details = procurement.ProcDetails?.ToList() ?? [],
                    Offers = procurement.ProcOffers?.ToList() ?? [],
                };
                viewModel.JobTypes = jobTypes;
                viewModel.Statuses = statuses;
                await PopulateEditUserSelectListsAsync(viewModel);

                var unitTypes = await _unitTypeRepository.GetActiveAsync();
                var jobTypeName = procurement.JobType?.TypeName ?? "Other";
                ViewBag.UnitTypes = unitTypes;
                ViewBag.SelectedJobTypeName = jobTypeName;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to load procurement for editing: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Procurements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Procurement.Edit)]
        public async Task<IActionResult> Edit(
            string id,
            ProcurementEditViewModel editViewModel,
            string? submitAction = "Created"
        )
        {
            if (id != editViewModel.ProcurementId)
            {
                ModelState.AddModelError("", "ID Procurement tidak sesuai");
                return NotFound();
            }

            // Check status-based authorization
            var procForAuthCheck = await _procurementService.GetProcurementByIdAsync(id);
            if (procForAuthCheck == null)
                return NotFound();
                
            if (!CanUserEditProcurementByStatus(procForAuthCheck))
            {
                TempData["ErrorMessage"] = "Anda tidak memiliki akses untuk mengedit procurement dengan status ini.";
                return RedirectToAction(nameof(Details), new { id });
            }

            RemoveDetailsValidation();
            RemoveOffersValidation();

            // Validasi JobTypeId
            if (string.IsNullOrWhiteSpace(editViewModel.JobTypeId))
            {
                ModelState.AddModelError(nameof(editViewModel.JobTypeId), "Job type wajib dipilih");
            }

            // Validasi ContractType
            if (editViewModel.ContractType == 0)
            {
                ModelState.AddModelError(
                    nameof(editViewModel.ContractType),
                    "Contract type wajib dipilih"
                );
            }

            // Validasi ProcurementCategory
            if (editViewModel.ProcurementCategory == 0)
            {
                ModelState.AddModelError(
                    nameof(editViewModel.ProcurementCategory),
                    "Jenis pengadaan wajib dipilih"
                );
            }

            // Validasi JobName
            if (string.IsNullOrWhiteSpace(editViewModel.JobName))
            {
                ModelState.AddModelError(
                    nameof(editViewModel.JobName),
                    "Nama pekerjaan wajib diisi"
                );
            }

            // Validasi StartDate dan EndDate
            if (editViewModel.StartDate == default)
            {
                ModelState.AddModelError(
                    nameof(editViewModel.StartDate),
                    "Tanggal mulai wajib diisi"
                );
            }

            if (editViewModel.EndDate == default)
            {
                ModelState.AddModelError(
                    nameof(editViewModel.EndDate),
                    "Tanggal selesai wajib diisi"
                );
            }

            if (
                editViewModel.StartDate != default
                && editViewModel.EndDate != default
                && editViewModel.EndDate < editViewModel.StartDate
            )
            {
                ModelState.AddModelError(
                    nameof(editViewModel.EndDate),
                    "Tanggal selesai harus setelah tanggal mulai"
                );
            }

            // Validasi RaNumber untuk Jasa
            if (
                editViewModel.ProcurementCategory == ProcurementCategory.Jasa
                && string.IsNullOrWhiteSpace(editViewModel.RaNumber)
            )
            {
                ModelState.AddModelError(
                    nameof(editViewModel.RaNumber),
                    "RA Number wajib diisi untuk jenis pengadaan jasa"
                );
            }

            // Validasi User IDs
            if (string.IsNullOrWhiteSpace(editViewModel.AnalystHteUserId))
            {
                ModelState.AddModelError(
                    nameof(editViewModel.AnalystHteUserId),
                    "Analyst HTE & LTS wajib dipilih"
                );
            }

            if (string.IsNullOrWhiteSpace(editViewModel.AssistantManagerUserId))
            {
                ModelState.AddModelError(
                    nameof(editViewModel.AssistantManagerUserId),
                    "Assistant Manager HTE wajib dipilih"
                );
            }

            if (string.IsNullOrWhiteSpace(editViewModel.ManagerUserId))
            {
                ModelState.AddModelError(
                    nameof(editViewModel.ManagerUserId),
                    "Manager Transport & Logistic wajib dipilih"
                );
            }

            // Determine status based on submitAction
            var status = await _procurementService.GetStatusByNameAsync(submitAction ?? "Created");
            if (status == null)
            {
                ModelState.AddModelError("", $"Status '{submitAction}' tidak ditemukan.");
            }
            else
            {
                editViewModel.StatusId = status.StatusId;
            }

            if (!ModelState.IsValid)
            {
                await RepopulateEditViewModel(editViewModel);
                return View(editViewModel);
            }

            try
            {
                // Ambil PicOpsUserId dari procurement existing (tidak boleh diubah)
                var existingProcurement = await _procurementService.GetProcurementByIdAsync(id);
                if (existingProcurement == null)
                {
                    TempData["ErrorMessage"] = "Procurement tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                var procurement = new Procurement
                {
                    ProcurementId = editViewModel.ProcurementId,
                    ProcNum = editViewModel.ProcNum,
                    JobTypeId = editViewModel.JobTypeId!,
                    ContractType = editViewModel.ContractType,
                    JobName = editViewModel.JobName!,
                    DocumentDate = editViewModel.DocumentDate,
                    StartDate = editViewModel.StartDate,
                    EndDate = editViewModel.EndDate,
                    ProjectRegion = editViewModel.ProjectRegion,
                    PotentialAccrualDate = editViewModel.PotentialAccrualDate,
                    SpkNumber = editViewModel.SpkNumber,
                    SpmpNumber = AppendSuffixIfNeeded(editViewModel.SpmpNumber), // Tambahkan suffix
                    MemoNumber = editViewModel.MemoNumber,
                    OeNumber = AppendSuffixIfNeeded(editViewModel.OeNumber), // Tambahkan suffix
                    RaNumber = editViewModel.RaNumber,
                    NoRig = editViewModel.NoRig,
                    NoHte = editViewModel.NoHte,
                    ProjectCode = editViewModel.ProjectCode,
                    Wonum = editViewModel.Wonum,
                    LtcName = editViewModel.LtcName,
                    Note = editViewModel.Note,
                    ProcurementCategory = editViewModel.ProcurementCategory,
                    PicOpsUserId = existingProcurement.PicOpsUserId, // Tetap gunakan nilai asli
                    AnalystHteUserId = editViewModel.AnalystHteUserId!,
                    AssistantManagerUserId = editViewModel.AssistantManagerUserId!,
                    ManagerUserId = editViewModel.ManagerUserId!,
                    StatusId = editViewModel.StatusId,
                };

                await _procurementService.EditProcurementAsync(
                    procurement,
                    id,
                    editViewModel.Details ?? [],
                    editViewModel.Offers ?? []
                );
                TempData["SuccessMessage"] = "Procurement updated successfully!";

                return RedirectToAction(nameof(Details), new { id = editViewModel.ProcurementId });
            }
            catch (KeyNotFoundException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                ModelState.AddModelError("", $"Data tidak ditemukan: {ex.Message}");
                return NotFound();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Data telah diubah oleh pengguna lain. Silakan refresh halaman dan coba lagi."
                );
                TempData["ErrorMessage"] =
                    $"Conflict: Data telah diubah oleh pengguna lain ({ex.InnerException?.Message ?? ex.Message})";
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    $"Gagal mengupdate data ke database: {ex.InnerException?.Message ?? ex.Message}"
                );
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", $"Operasi tidak valid: {ex.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    $"Terjadi kesalahan saat mengupdate data: {ex.Message}"
                );
            }

            await RepopulateEditViewModel(editViewModel);
            return View(editViewModel);
        }

        // GET: Procurements/Delete/5
        [Authorize(Policy = Permissions.Procurement.Delete)]
        public async Task<IActionResult> Delete(string id)
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
                TempData["ErrorMessage"] = $"Failed to load procurement for deletion: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Procurements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Procurement.Delete)]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "ID Procurement tidak valid";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var procurement = await _procurementService.GetProcurementByIdAsync(id);
                if (procurement == null)
                {
                    TempData["ErrorMessage"] = "Procurement tidak ditemukan";
                    return NotFound();
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                
                // Get PrId before deleting procurement to recalculate PR status
                var prId = procurement.PrId;
                
                // Soft delete all related documents
                var deletedDocsCount = await _procDocService.DeleteAllByProcurementAsync(id, currentUserId);
                
                // Soft delete profit loss if exists
                await _pnlService.DeleteByProcurementAsync(id, currentUserId);
                
                // Soft delete procurement
                await _procurementService.DeleteProcurementAsync(procurement, currentUserId);
                
                // Recalculate PR status if procurement was linked to a PR
                if (!string.IsNullOrEmpty(prId))
                {
                    await _trackingService.RecalculatePrStatusAsync(prId);
                }
                
                var message = deletedDocsCount > 0
                    ? $"Procurement beserta {deletedDocsCount} dokumen terkait berhasil dihapus!"
                    : "Procurement berhasil dihapus!";
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] =
                    $"Gagal menghapus procurement. Data ini mungkin masih digunakan di tempat lain: {ex.InnerException?.Message ?? ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = $"Operasi tidak valid: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    $"Terjadi kesalahan saat menghapus procurement: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Procurements/Publish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Procurement.Edit)]
        public async Task<IActionResult> Publish(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "ID Procurement tidak valid";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Cek apakah procurement memiliki Profit & Loss
                var profitLoss = await _pnlService.GetByProcurementAsync(id);
                if (profitLoss == null)
                {
                    TempData["ErrorMessage"] =
                        "Tidak dapat publish procurement. Buat Profit & Loss terlebih dahulu.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Get procurement details before publishing for notification
                var procurement = await _procurementService.GetProcurementByIdAsync(id);

                await _procurementService.PublishAsync(id);

                // Send notification to all AP-PO users
                if (procurement != null)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    var publisherName =
                        currentUser?.FullName ?? currentUser?.UserName ?? "Operator";

                    await _notificationService.NotifyProcurementPublishedAsync(
                        id,
                        procurement.ProcNum ?? "-",
                        currentUser?.Id ?? "",
                        publisherName,
                        HttpContext.RequestAborted
                    );
                }

                TempData["SuccessMessage"] =
                    "Procurement berhasil dipublish dan status berubah menjadi 'Waiting Pickup'.";
            }
            catch (KeyNotFoundException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    $"Terjadi kesalahan saat publish procurement: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Procurements/Unpublish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Procurement.Edit)]
        public async Task<IActionResult> Unpublish(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "ID Procurement tidak valid";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _procurementService.UnpublishAsync(id);
                TempData["SuccessMessage"] =
                    "Publish procurement berhasil dibatalkan. Status kembali menjadi 'Created'.";
            }
            catch (KeyNotFoundException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    $"Terjadi kesalahan saat membatalkan publish procurement: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        //GET: Procurements/5/CreatePnl
        [HttpGet("Procurements/{procurementId}/CreateProfitLoss")]
        public async Task<IActionResult> CreateProfitLoss(string procurementId)
        {
            if (string.IsNullOrWhiteSpace(procurementId))
            {
                TempData["ErrorMessage"] = "Procurement ID tidak valid";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var procurement = await _procurementService.GetProcurementByIdAsync(procurementId);
                if (procurement == null)
                {
                    TempData["ErrorMessage"] = "Procurement tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                // Validasi apakah procurement sudah memiliki profit loss
                var existingPnl = await _pnlService.GetByProcurementAsync(procurementId);
                if (existingPnl != null)
                {
                    TempData["WarningMessage"] =
                        "Procurement ini sudah memiliki Profit & Loss. Anda akan mengedit data yang sudah ada.";
                    return RedirectToAction(
                        nameof(EditProfitLoss),
                        new { id = existingPnl.ProfitLossId }
                    );
                }

                var vendors = await _vendorService.GetAllVendorsAsync();
                if (vendors == null || !vendors.Any())
                {
                    TempData["ErrorMessage"] =
                        "Tidak ada vendor yang tersedia. Tambahkan vendor terlebih dahulu.";
                    return RedirectToAction(nameof(Details), new { id = procurementId });
                }

                var viewModel = new ProfitLossInputViewModel
                {
                    ProcurementId = procurementId,
                    JobTypeName = procurement.JobType?.TypeName,
                    VendorChoices = vendors
                        .Select(vendor => new VendorChoiceViewModel
                        {
                            Id = vendor.VendorId,
                            Name = vendor.VendorName,
                        })
                        .ToList(),
                    OfferItems = (procurement.ProcOffers ?? [])
                        .Select(o => new ProcOfferLiteVm
                        {
                            ProcOfferId = o.ProcOfferId,
                            ItemPenawaran = o.ItemPenawaran,
                            Quantity = o.Qty,
                            Unit = o.Unit,
                            UnitRevenue = o.UnitRevenue,
                        })
                        .ToList(),
                };

                viewModel.Items = viewModel
                    .OfferItems.Select(o => new ItemTariffInputVm
                    {
                        ProcOfferId = o.ProcOfferId,
                        Quantity = (int)Math.Round(o.Quantity),
                        TarifAwal = null,
                        TarifAdd = null,
                        KmPer25 = null,
                        OperatorCost = null,
                    })
                    .ToList();

                ViewBag.ProcNum = procurement.ProcNum;
                ViewBag.IssueDate = procurement.CreatedAt.ToString("d MMMM yyyy");
                ViewBag.JobTypeName = procurement.JobType?.TypeName ?? "Unknown";
                ViewBag.UnitTypes = await _unitTypeRepository.GetActiveAsync();

                if (viewModel.OfferItems.Count == 0)
                {
                    TempData["WarningMessage"] =
                        "Tidak ada item penawaran (ProcOffer). Tambahkan item penawaran di halaman Edit Procurement terlebih dahulu untuk dapat menginput harga per item.";
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfitLossPost(
            [FromForm] ProfitLossInputViewModel viewModel
        )
        {
            var offerKeys = Request.Form["Items.Index"].ToArray();
            foreach (var key in offerKeys)
            {
                ModelState.Remove($"Items[{key}].ProcOfferId");
            }

            // Backfill OfferItems and quantities early so validation uses default values when user didn't enter them
            if (!string.IsNullOrWhiteSpace(viewModel.ProcurementId))
            {
                await RepopulateVendorChoices(viewModel);
            }

            // Validasi ProcurementId
            if (string.IsNullOrWhiteSpace(viewModel.ProcurementId))
            {
                ModelState.AddModelError(
                    nameof(viewModel.ProcurementId),
                    "Procurement ID tidak valid"
                );
            }

            // Validasi Items
            if (viewModel.Items == null || viewModel.Items.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.Items),
                    "Minimal harus ada 1 item tarif untuk membuat Profit & Loss"
                );
            }
            else
            {
                // Validasi setiap item
                for (int i = 0; i < viewModel.Items.Count; i++)
                {
                    var item = viewModel.Items[i];

                    if (string.IsNullOrWhiteSpace(item.ProcOfferId))
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].ProcOfferId",
                            "Proc Offer ID tidak boleh kosong"
                        );
                    }

                    if (item.Quantity <= 0)
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].Quantity",
                            "Quantity harus lebih dari 0"
                        );
                    }

                    if (item.TarifAwal < 0)
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].TarifAwal",
                            "Tarif Awal tidak boleh negatif"
                        );
                    }

                    if (item.TarifAdd < 0)
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].TarifAdd",
                            "Tarif Add tidak boleh negatif"
                        );
                    }

                    if (item.OperatorCost < 0)
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].OperatorCost",
                            "Operator Cost tidak boleh negatif"
                        );
                    }
                }
            }

            // Validasi AccrualAmount
            if (viewModel.AccrualAmount.HasValue && viewModel.AccrualAmount.Value < 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.AccrualAmount),
                    "Accrual Amount tidak boleh negatif"
                );
            }

            // Validasi RealizationAmount
            if (viewModel.RealizationAmount.HasValue && viewModel.RealizationAmount.Value < 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.RealizationAmount),
                    "Realization Amount tidak boleh negatif"
                );
            }

            // Validasi Distance
            if (viewModel.Distance < 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.Distance),
                    "Distance tidak boleh negatif"
                );
            }

            if (!ModelState.IsValid)
            {
                await RepopulateVendorChoices(viewModel);
                return View("CreateProfitLoss", viewModel);
            }

            var selectedVendors = viewModel.SelectedVendorIds?.Distinct().ToList() ?? [];
            if (selectedVendors.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.SelectedVendorIds),
                    "Pilih minimal 1 vendor untuk membuat Profit & Loss"
                );
                await RepopulateVendorChoices(viewModel);
                return View(viewModel);
            }

            // Validasi Vendor Offers
            if (viewModel.Vendors != null)
            {
                foreach (var vendor in viewModel.Vendors)
                {
                    if (string.IsNullOrWhiteSpace(vendor.VendorId))
                    {
                        ModelState.AddModelError(
                            nameof(viewModel.Vendors),
                            "Vendor ID tidak boleh kosong"
                        );
                        continue;
                    }

                    if (vendor.Items == null || vendor.Items.Count == 0)
                    {
                        ModelState.AddModelError(
                            nameof(viewModel.Vendors),
                            $"Vendor harus memiliki minimal 1 item penawaran"
                        );
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await RepopulateVendorChoices(viewModel);
                return View(viewModel);
            }

            try
            {
                var dto = MapToDto(viewModel, selectedVendors);
                var pnl = await _pnlService.SaveInputAndCalculateAsync(dto);

                if (pnl == null)
                {
                    ModelState.AddModelError("", "Gagal menyimpan Profit & Loss. Result is null.");
                    await RepopulateVendorChoices(viewModel);
                    return View("CreateProfitLoss", viewModel);
                }

                await UploadRoundLettersAsync(viewModel, pnl?.ProfitLossId);

                var procurement =
                    await _procurementService.GetProcurementByIdAsync(dto.ProcurementId)
                    ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

                var pdfBytes = await _documentGenerator.GenerateProfitLossAsync(procurement);

                var docTypes = await _docTypeService.GetAllDocumentTypesAsync(
                    page: 1,
                    pageSize: 200,
                    search: null,
                    fields: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name" },
                    ct: default
                );

                var pnlDocTypeId =
                    docTypes
                        .Items.FirstOrDefault(doc =>
                            doc.Name.Equals("Profit & Loss", StringComparison.OrdinalIgnoreCase)
                        )
                        ?.DocumentTypeId
                    ?? throw new InvalidOperationException(
                        "DocumentType 'Profit & Loss' tidak ditemukan"
                    );

                var generateReq = new GeneratedProcDocumentRequest
                {
                    ProcurementId = dto.ProcurementId,
                    DocumentTypeId = pnlDocTypeId,
                    Bytes = pdfBytes, // FIX: Tambahkan bytes yang sudah digenerate
                    FileName = $"Profit_Loss_{procurement.ProcNum}.pdf",
                    ContentType = "application/pdf",
                    Description = "Profit & Loss auto-generated",
                    GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    CreatedAt = DateTime.Now,
                };

                var saveResult = await _procDocService.SaveGeneratedAsync(generateReq);

                // Auto-generate Surat Perintah Mulai Pekerjaan (SPMP)
                var spmpDocTypeId =
                    docTypes
                        .Items.FirstOrDefault(doc =>
                            doc.Name.Equals("Surat Perintah Mulai Pekerjaan (SPMP)", StringComparison.OrdinalIgnoreCase)
                        )
                        ?.DocumentTypeId;

                if (!string.IsNullOrEmpty(spmpDocTypeId))
                {
                    var spmpPdfBytes = await _documentGenerator.GenerateSPMPAsync(procurement);
                    var spmpGenerateReq = new GeneratedProcDocumentRequest
                    {
                        ProcurementId = dto.ProcurementId,
                        DocumentTypeId = spmpDocTypeId,
                        Bytes = spmpPdfBytes,
                        FileName = $"SPMP_{procurement.ProcNum}.pdf",
                        ContentType = "application/pdf",
                        Description = "Surat Perintah Mulai Pekerjaan (SPMP) auto-generated from P&L calculation",
                        GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        CreatedAt = DateTime.Now,
                    };
                    await _procDocService.SaveGeneratedAsync(spmpGenerateReq);
                }

                TempData["SuccessMessage"] =
                    "Profit & Loss created successfully and documents (including SPMP) were generated.";

                return RedirectToAction(nameof(Details), new { id = dto.ProcurementId });
            }
            catch (KeyNotFoundException ex)
            {
                ModelState.AddModelError("", $"Data tidak ditemukan: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", $"Operasi tidak valid: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    $"Gagal menyimpan data ke database: {ex.InnerException?.Message ?? ex.Message}"
                );
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", $"Validasi gagal: {ex.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Terjadi kesalahan: {ex.Message}");
            }

            await RepopulateVendorChoices(viewModel);
            return View("CreateProfitLoss", viewModel);
        }

        // GET: /Procurements/EditPnL/5
        [HttpGet]
        public async Task<IActionResult> EditProfitLoss(string id)
        {
            try
            {
                var dto = await _pnlService.GetEditDataAsync(id);
                var dtoItems = dto.Items ?? [];
                var dtoVendors = dto.Vendors ?? [];

                var vendors = await _vendorService.GetAllVendorsAsync();
                var procurement =
                    await _procurementService.GetProcurementByIdAsync(dto.ProcurementId)
                    ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

                var selectedVendorIds = (dto.SelectedVendorIds ?? [])
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (selectedVendorIds.Count == 0)
                {
                    selectedVendorIds = dtoVendors
                        .Select(v => v.VendorId)
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var selectedVendorSet = new HashSet<string>(
                    selectedVendorIds,
                    StringComparer.OrdinalIgnoreCase
                );

                var vendorLookup = dtoVendors
                    .Where(v => !string.IsNullOrWhiteSpace(v.VendorId))
                    .GroupBy(v => v.VendorId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                // Get all current ProcOffers to ensure vendor items include new offers
                var allProcOfferIds = (procurement.ProcOffers ?? [])
                    .Select(x => x.ProcOfferId)
                    .ToList();

                var vendorModels = vendorLookup
                    .Select(kvp =>
                    {
                        var vendorEntries = kvp.Value;

                        // Get existing vendor items
                        var existingVendorItems = vendorEntries
                            .SelectMany(v => v.Items ?? [])
                            .GroupBy(it => it.ProcOfferId, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Last(),
                                StringComparer.OrdinalIgnoreCase
                            );

                        // Build items list including both existing and new ProcOffers
                        var aggregatedItems = allProcOfferIds
                            .Select(procOfferId =>
                            {
                                if (existingVendorItems.TryGetValue(procOfferId, out var existing))
                                {
                                    // Use existing vendor item data (preserves IsIncluded state)
                                    return new VendorOfferPerItemInputVm
                                    {
                                        ProcOfferId = existing.ProcOfferId,
                                        Prices = existing.Prices?.ToList() ?? [],
                                        Quantity = existing.Quantity,
                                        Trip = existing.Trip,
                                        IsIncluded = existing.IsIncluded, // ✅ Preserve checked/unchecked state
                                    };
                                }
                                else
                                {
                                    // Create empty entry for new ProcOffer
                                    // IsIncluded = true ensures JavaScript calculation works properly
                                    // User can uncheck items they don't want to include
                                    return new VendorOfferPerItemInputVm
                                    {
                                        ProcOfferId = procOfferId,
                                        Prices = [],
                                        Quantity = 0,
                                        Trip = 0,
                                        IsIncluded = true, // ✅ Checked by default for proper calculation
                                    };
                                }
                            })
                            .ToList();

                        var letters = vendorEntries.First().Letters?.ToList() ?? [];
                        var letterDocs = (vendorEntries.First().LetterDocIds ?? [])
                            .Select(x => x ?? string.Empty)
                            .ToList();

                        return new VendorItemOfferInputVm
                        {
                            VendorId = vendorEntries.First().VendorId,
                            Letters = letters,
                            LetterDocIds = letterDocs,
                            Items = aggregatedItems,
                        };
                    })
                    .ToList();

                // Build OfferItems from all current ProcOffers
                var offerItems = (procurement.ProcOffers ?? [])
                    .Select(x => new ProcOfferLiteVm
                    {
                        ProcOfferId = x.ProcOfferId,
                        ItemPenawaran = x.ItemPenawaran,
                        Quantity = x.Qty,
                        Unit = x.Unit,
                        UnitRevenue = x.UnitRevenue,
                    })
                    .ToList();

                // Create a map of existing ProfitLossItems by ProcOfferId
                var existingItemsMap = dtoItems.ToDictionary(
                    x => x.ProcOfferId,
                    StringComparer.OrdinalIgnoreCase
                );

                // Build Items list: include existing items AND create empty entries for new ProcOffers
                var items = offerItems
                    .Select(offer =>
                    {
                        // If item already exists in ProfitLoss, use existing data
                        if (existingItemsMap.TryGetValue(offer.ProcOfferId, out var existingItem))
                        {
                            return new ItemTariffInputVm
                            {
                                ProcOfferId = existingItem.ProcOfferId,
                                Quantity = existingItem.Quantity,
                                QtyItems = existingItem.QtyItems,
                                TarifAwal = existingItem.TarifAwal,
                                TarifAdd = existingItem.TarifAdd,
                                KmPer25 = existingItem.KmPer25,
                                OperatorCost = existingItem.OperatorCost,
                                UnitRevenue = offer.UnitRevenue,
                            };
                        }
                        // Otherwise, create new empty item for the new ProcOffer
                        else
                        {
                            return new ItemTariffInputVm
                            {
                                ProcOfferId = offer.ProcOfferId,
                                Quantity = (int)offer.Quantity,
                                QtyItems = (int)offer.Quantity,
                                TarifAwal = 0m,
                                TarifAdd = 0m,
                                KmPer25 = 0m,
                                OperatorCost = 0m,
                                UnitRevenue = offer.UnitRevenue,
                            };
                        }
                    })
                    .ToList();

                var vm = new ProfitLossEditViewModel
                {
                    ProfitLossId = dto.ProfitLossId,
                    ProcurementId = dto.ProcurementId,
                    JobTypeName = procurement.JobType?.TypeName,
                    AccrualAmount = dto.AccrualAmount,
                    RealizationAmount = dto.RealizationAmount,
                    Distance = dto.Distance,
                    TglMulaiSewa = dto.TglMulaiSewa,
                    TglMulaiMoving = dto.TglMulaiMoving,
                    Items = items,
                    Vendors = vendorModels,
                    SelectedVendorIds = selectedVendorIds,
                    VendorChoices = vendors
                        .Select(v => new VendorChoiceViewModel
                        {
                            Id = v.VendorId,
                            Name = v.VendorName,
                        })
                        .ToList(),
                    OfferItems = offerItems,
                };

                ViewBag.ProcNum = procurement.ProcNum;
                ViewBag.IssueDate = procurement.CreatedAt.ToString("d MMMM yyyy");
                ViewBag.JobTypeName = procurement.JobType?.TypeName ?? "Unknown";
                ViewBag.UnitTypes = await _unitTypeRepository.GetActiveAsync();
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
                return View("CreateProfitLoss", vm);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfitLoss(ProfitLossEditViewModel viewModel)
        {
            // Validasi ProfitLossId
            if (string.IsNullOrWhiteSpace(viewModel.ProfitLossId))
            {
                ModelState.AddModelError(
                    nameof(viewModel.ProfitLossId),
                    "Profit Loss ID tidak valid"
                );
            }

            // Validasi ProcurementId
            if (string.IsNullOrWhiteSpace(viewModel.ProcurementId))
            {
                ModelState.AddModelError(
                    nameof(viewModel.ProcurementId),
                    "Procurement ID tidak valid"
                );
            }

            // Validasi Items
            if (viewModel.Items == null || viewModel.Items.Count == 0)
            {
                ModelState.AddModelError(nameof(viewModel.Items), "Minimal harus ada 1 item tarif");
            }
            else
            {
                // Validasi setiap item
                for (int i = 0; i < viewModel.Items.Count; i++)
                {
                    var item = viewModel.Items[i];

                    if (string.IsNullOrWhiteSpace(item.ProcOfferId))
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].ProcOfferId",
                            "Proc Offer ID tidak boleh kosong"
                        );
                    }

                    if (item.Quantity <= 0)
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].Quantity",
                            "Quantity harus lebih dari 0"
                        );
                    }

                    if (item.TarifAwal < 0)
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].TarifAwal",
                            "Tarif Awal tidak boleh negatif"
                        );
                    }

                    if (item.TarifAdd < 0)
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].TarifAdd",
                            "Tarif Add tidak boleh negatif"
                        );
                    }

                    if (item.OperatorCost < 0)
                    {
                        ModelState.AddModelError(
                            $"Items[{i}].OperatorCost",
                            "Operator Cost tidak boleh negatif"
                        );
                    }
                }
            }

            // Validasi AccrualAmount
            if (viewModel.AccrualAmount.HasValue && viewModel.AccrualAmount.Value < 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.AccrualAmount),
                    "Accrual Amount tidak boleh negatif"
                );
            }

            // Validasi RealizationAmount
            if (viewModel.RealizationAmount.HasValue && viewModel.RealizationAmount.Value < 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.RealizationAmount),
                    "Realization Amount tidak boleh negatif"
                );
            }

            // Validasi Distance
            if (viewModel.Distance < 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.Distance),
                    "Distance tidak boleh negatif"
                );
            }

            // Validasi SelectedVendorIds
            if (viewModel.SelectedVendorIds == null || viewModel.SelectedVendorIds.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.SelectedVendorIds),
                    "Pilih minimal 1 vendor"
                );
            }

            if (!ModelState.IsValid)
            {
                await RepopulateVendorChoices(viewModel);
                var procurement = await _procurementService.GetProcurementByIdAsync(
                    viewModel.ProcurementId
                );
                if (procurement != null)
                {
                    ViewBag.ProcNum = procurement.ProcNum;
                    ViewBag.IssueDate = procurement.CreatedAt.ToString("d MMMM yyyy");
                }

                return View("CreateProfitLoss", viewModel);
            }

            var distinctSelectedVendors = (viewModel.SelectedVendorIds ?? [])
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinctSelectedVendors.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.SelectedVendorIds),
                    "Tidak ada vendor valid yang dipilih"
                );
                await RepopulateVendorChoices(viewModel);
                return View("CreateProfitLoss", viewModel);
            }

            try
            {
                var update = new ProfitLossUpdateDto
                {
                    ProfitLossId = viewModel.ProfitLossId,
                    ProcurementId = viewModel.ProcurementId,
                    AccrualAmount = viewModel.AccrualAmount,
                    RealizationAmount = viewModel.RealizationAmount,
                    Distance = viewModel.Distance,
                    TglMulaiSewa = viewModel.TglMulaiSewa,
                    TglMulaiMoving = viewModel.TglMulaiMoving,
                    Items = (viewModel.Items ?? [])
                        .Select(x => new ProfitLossItemInputDto
                        {
                            ProcOfferId = x.ProcOfferId,
                            Quantity = x.Quantity,
                            QtyItems = x.QtyItems,
                            TarifAwal = x.TarifAwal ?? 0m,
                            TarifAdd = x.TarifAdd ?? 0m,
                            KmPer25 = x.KmPer25 ?? 0m,
                            OperatorCost = x.OperatorCost ?? 0m,
                            UnitRevenue = x.UnitRevenue,
                        })
                        .ToList(),
                    SelectedVendorIds = distinctSelectedVendors,
                };

                var allowedVendorSet = new HashSet<string>(
                    distinctSelectedVendors,
                    StringComparer.OrdinalIgnoreCase
                );
                update.Vendors = BuildVendorOfferDtos(viewModel.Vendors, allowedVendorSet);

                var pnlUpdated = await _pnlService.EditProfitLossAsync(update);
                await UploadRoundLettersAsync(viewModel, pnlUpdated.ProfitLossId);

                // Auto-generate Surat Perintah Mulai Pekerjaan (SPMP) after P&L update
                var procurement = await _procurementService.GetProcurementByIdAsync(viewModel.ProcurementId)
                    ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

                var docTypes = await _docTypeService.GetAllDocumentTypesAsync(
                    page: 1,
                    pageSize: 200,
                    search: null,
                    fields: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name" },
                    ct: default
                );

                var spmpDocTypeId =
                    docTypes
                        .Items.FirstOrDefault(doc =>
                            doc.Name.Equals("Surat Perintah Mulai Pekerjaan (SPMP)", StringComparison.OrdinalIgnoreCase)
                        )
                        ?.DocumentTypeId;

                if (!string.IsNullOrEmpty(spmpDocTypeId))
                {
                    var spmpPdfBytes = await _documentGenerator.GenerateSPMPAsync(procurement);
                    var spmpGenerateReq = new GeneratedProcDocumentRequest
                    {
                        ProcurementId = viewModel.ProcurementId,
                        DocumentTypeId = spmpDocTypeId,
                        Bytes = spmpPdfBytes,
                        FileName = $"SPMP_{procurement.ProcNum}.pdf",
                        ContentType = "application/pdf",
                        Description = "Surat Perintah Mulai Pekerjaan (SPMP) auto-generated from P&L update",
                        GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        CreatedAt = DateTime.Now,
                    };
                    await _procDocService.SaveGeneratedAsync(spmpGenerateReq);
                }

                TempData["SuccessMessage"] = "Profit & Loss updated successfully and SPMP document was generated.";
                return RedirectToAction(nameof(Details), new { id = viewModel.ProcurementId });
            }
            catch (KeyNotFoundException ex)
            {
                ModelState.AddModelError("", $"Data tidak ditemukan: {ex.Message}");
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ModelState.AddModelError(
                    "",
                    "Data telah diubah oleh pengguna lain. Silakan refresh halaman dan coba lagi."
                );
                TempData["ErrorMessage"] =
                    $"Conflict: Data telah diubah ({ex.InnerException?.Message ?? ex.Message})";
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    $"Gagal mengupdate data ke database: {ex.InnerException?.Message ?? ex.Message}"
                );
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", $"Operasi tidak valid: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", $"Validasi gagal: {ex.Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Terjadi kesalahan: {ex.Message}");
            }

            await RepopulateVendorChoices(viewModel);
            var proc = await _procurementService.GetProcurementByIdAsync(viewModel.ProcurementId);
            if (proc != null)
            {
                ViewBag.ProcNum = proc.ProcNum;
                ViewBag.IssueDate = proc.CreatedAt.ToString("d MMMM yyyy");
            }
            return View("CreateProfitLoss", viewModel);
        }

        #endregion

        #region Procurement Tracking Operations

        // POST: Procurements/SendApproval/{procurementId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendApproval(string procurementId, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User tidak teridentifikasi.";
                return RedirectToAction(nameof(Details), new { id = procurementId });
            }

            var result = await _trackingService.SendForApprovalAsync(procurementId, userId, ct);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id = procurementId });
        }

        // GET: Procurements/GenerateQrCode/{procurementId}
        [HttpGet]
        public async Task<IActionResult> GenerateQrCode(string procurementId, CancellationToken ct)
        {
            var tracking = await _trackingService.GetTrackingByProcurementIdAsync(procurementId, ct);

            if (tracking == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(tracking.ApprovalToken))
            {
                return BadRequest("Approval token tidak tersedia.");
            }

            // Generate QR code using local library (no external API)
            var deepLink = $"procurehte://approve/{tracking.ApprovalToken}";
            var pngBytes = _qrCodeGenerator.GenerateAsPng(deepLink, 10);

            return File(pngBytes, "image/png", "approval-qr.png");
        }

        #endregion

        #region Helper Methods

        private const string REFERENCE_NUMBER_PREFIX = "/PDC-1110/";
        private const string REFERENCE_NUMBER_SUFFIX_END = "-S0";

        private static string GetCurrentYearSuffix() => 
            $"{REFERENCE_NUMBER_PREFIX}{DateTime.Now.Year}{REFERENCE_NUMBER_SUFFIX_END}";

        private static string? AppendSuffixIfNeeded(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            var trimmed = value.Trim();

            // Jika sudah ada suffix pattern (any year), return as is
            if (HasReferenceNumberSuffix(trimmed))
                return trimmed;

            // Tambahkan suffix dengan tahun saat ini
            return trimmed + GetCurrentYearSuffix();
        }

        private static bool HasReferenceNumberSuffix(string value)
        {
            // Pattern: /PDC-1110/YYYY-S0 dimana YYYY adalah 4 digit tahun
            var pattern = $"{REFERENCE_NUMBER_PREFIX}\\d{{4}}{REFERENCE_NUMBER_SUFFIX_END}$";
            return System.Text.RegularExpressions.Regex.IsMatch(value, pattern);
        }

        private static string? RemoveSuffixIfNeeded(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            var trimmed = value.Trim();

            // Remove suffix pattern /PDC-1110/YYYY-S0 untuk tahun apapun
            var pattern = $"{REFERENCE_NUMBER_PREFIX}\\d{{4}}{REFERENCE_NUMBER_SUFFIX_END}$";
            return System.Text.RegularExpressions.Regex.Replace(trimmed, pattern, "");
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "file";
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var ch in fileName.Trim())
            {
                sb.Append(invalid.Contains(ch) ? '_' : ch);
            }
            return sb.ToString();
        }

        private static ProfitLossSummaryViewModel MapToViewModel(ProfitLossSummaryDto dto)
        {
            return new ProfitLossSummaryViewModel
            {
                ProfitLossId = dto.ProfitLossId,
                ProcurementId = dto.ProcurementId,

                TotalOperatorCost = dto.TotalOperatorCost,
                TotalRevenue = dto.TotalRevenue,
                AccrualAmount = dto.AccrualAmount,
                RealizationAmount = dto.RealizationAmount,
                Distance = dto.Distance,

                SelectedVendorId = dto.SelectedVendorId,
                SelectedVendorName = dto.SelectedVendorName,
                SelectedFinalOffer = dto.SelectedFinalOffer,
                Profit = dto.Profit,
                ProfitPercent = dto.ProfitPercent,

                CreatedAt = dto.CreatedAt,

                Items = dto.Items,
                SelectedVendorNames = dto.SelectedVendorNames?.ToList() ?? [],

                Rows = dto
                    .VendorComparisons.Select(v =>
                        (v.VendorName, v.FinalOffer, v.Profit, v.ProfitPercent, v.IsSelected)
                    )
                    .ToList(),
            };
        }

        private static ProfitLossInputDto MapToDto(
            ProfitLossInputViewModel vm,
            List<string> selectedVendors
        )
        {
            // Backfill Quantity from OfferItems when user didn't provide Quantity/Durasi
            var qtyMap = (vm.OfferItems ?? [])
                .Where(o => !string.IsNullOrWhiteSpace(o.ProcOfferId))
                .ToDictionary(
                    o => o.ProcOfferId,
                    o => o.Quantity,
                    StringComparer.OrdinalIgnoreCase
                );

            var items = (vm.Items ?? [])
                .Select(x => new ProfitLossItemInputDto
                {
                    ProcOfferId = x.ProcOfferId,
                    Quantity =
                        x.Quantity > 0
                            ? x.Quantity
                            : (qtyMap.TryGetValue(x.ProcOfferId, out var q) ? (int)q : x.Quantity),
                    QtyItems =
                        x.QtyItems > 0
                            ? x.QtyItems
                            : (qtyMap.TryGetValue(x.ProcOfferId, out var qi) ? (int)qi : 1),
                    TarifAwal = x.TarifAwal ?? 0m,
                    TarifAdd = x.TarifAdd ?? 0m,
                    KmPer25 = x.KmPer25 ?? 0m,
                    OperatorCost = x.OperatorCost ?? 0m,
                    UnitRevenue = x.UnitRevenue,
                })
                .ToList();

            // langsung baca bentuk nested dari VM
            var allowedVendors = new HashSet<string>(
                selectedVendors,
                StringComparer.OrdinalIgnoreCase
            );
            var vendorItemOffers = BuildVendorOfferDtos(vm.Vendors, allowedVendors);

            return new ProfitLossInputDto
            {
                ProcurementId = vm.ProcurementId,
                AccrualAmount = vm.AccrualAmount,
                RealizationAmount = vm.RealizationAmount,
                Distance = vm.Distance,
                TglMulaiSewa = vm.TglMulaiSewa,
                TglMulaiMoving = vm.TglMulaiMoving,
                Items = items,
                SelectedVendorIds = selectedVendors,
                Vendors = vendorItemOffers,
            };
        }

        private static List<VendorItemOffersDto> BuildVendorOfferDtos(
            IEnumerable<VendorItemOfferInputVm>? vendorInputs,
            HashSet<string>? allowedVendorIds
        )
        {
            var result = new List<VendorItemOffersDto>();
            if (vendorInputs == null)
                return result;

            foreach (var vendor in vendorInputs)
            {
                var vendorId = vendor?.VendorId;
                if (string.IsNullOrWhiteSpace(vendorId))
                    continue;

                if (allowedVendorIds != null && !allowedVendorIds.Contains(vendorId))
                    continue;

                var dtoItems = new List<VendorOfferPerItemDto>();

                foreach (var item in vendor!.Items ?? Enumerable.Empty<VendorOfferPerItemInputVm>())
                {
                    if (string.IsNullOrWhiteSpace(item.ProcOfferId))
                        continue;

                    // Skip items that are explicitly excluded from the offer
                    if (!item.IsIncluded)
                        continue;

                    var dto = new VendorOfferPerItemDto
                    {
                        VendorId = vendorId,
                        ProcOfferId = item.ProcOfferId!,
                        Round = item.Round,
                        Prices = [],
                        Quantity = item.Quantity,
                        Trip = item.Trip,
                    };

                    var prices = item.Prices ?? [];

                    for (int idx = 0; idx < prices.Count; idx++)
                    {
                        var price = prices[idx];
                        if (price <= 0m)
                            continue;
                        dto.Prices.Add(price);
                    }

                    if (dto.Prices.Count > 0)
                        dtoItems.Add(dto);
                }

                if (dtoItems.Count > 0)
                {
                    result.Add(
                        new VendorItemOffersDto
                        {
                            VendorId = vendorId,
                            Items = dtoItems,
                            Letters = (vendor.Letters ?? [])
                                .Select(l => l?.Trim() ?? string.Empty)
                                .ToList(),
                            LetterDocIds = (vendor.LetterDocIds ?? [])
                                .Where(x => x != null)
                                .ToList()!,
                        }
                    );
                }
            }

            return result;
        }

        private async Task UploadRoundLettersAsync(
            ProfitLossInputViewModel vm,
            string? profitLossId
        )
        {
            if (vm?.Vendors == null || vm.Vendors.Count == 0)
                return;

            var docTypes = await _docTypeService.GetAllDocumentTypesAsync(
                page: 1,
                pageSize: 200,
                search: null,
                fields: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Name" },
                ct: default
            );

            var sphDocTypeId =
                docTypes
                    .Items.FirstOrDefault(d =>
                        d.Name.Equals("Surat Penawaran Harga", StringComparison.OrdinalIgnoreCase)
                    )
                    ?.DocumentTypeId
                ?? throw new InvalidOperationException(
                    "DocumentType 'Surat Penawaran Harga' tidak ditemukan."
                );

            var snhDocTypeId =
                docTypes
                    .Items.FirstOrDefault(d =>
                        d.Name.Equals("Surat Negosiasi Harga", StringComparison.OrdinalIgnoreCase)
                    )
                    ?.DocumentTypeId
                ?? throw new InvalidOperationException(
                    "DocumentType 'Surat Negosiasi Harga' tidak ditemukan."
                );

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var vendorFileLookup = BuildLetterFileLookup();

            for (var vendorIndex = 0; vendorIndex < vm.Vendors.Count; vendorIndex++)
            {
                var vendor = vm.Vendors[vendorIndex];
                if (vendor == null || string.IsNullOrWhiteSpace(vendor.VendorId))
                    continue;

                var letters = vendor.Letters ?? [];
                var docIds = vendor.LetterDocIds ?? [];
                var deletes = vendor.LetterDeletes ?? [];
                var files = MergeLetterFiles(vendor.LetterFiles, vendorFileLookup, vendorIndex);

                var maxLength = Math.Max(
                    Math.Max(letters.Count, files.Count),
                    Math.Max(docIds.Count, deletes.Count)
                );

                for (int i = 0; i < maxLength; i++)
                {
                    var letter = i < letters.Count ? letters[i] : null;
                    var file = i < files.Count ? files[i] : null;
                    var docId = i < docIds.Count ? docIds[i] : null;
                    var deleteFlag = i < deletes.Count && deletes[i];

                    var round = i + 1;
                    var docTypeId = round == 1 ? sphDocTypeId : snhDocTypeId;
                    var docLabel = round == 1 ? "Surat Penawaran Harga" : "Surat Negosiasi Harga";
                    var prefix = round == 1 ? "SPH" : "SNH";

                    var hasExistingDoc = !string.IsNullOrWhiteSpace(docId);
                    var hasNewFile = file != null && file.Length > 0;

                    if (hasExistingDoc && (deleteFlag || hasNewFile))
                    {
                        try
                        {
                            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                            await _procDocService.DeleteAsync(docId!, currentUserId);
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError(
                                "",
                                $"Gagal menghapus dokumen SPH/SNH {docId ?? "-"}: {ex.Message}"
                            );
                        }
                        await _roundLetterRepository.DeleteByProcDocumentIdAsync(
                            docId!,
                            HttpContext.RequestAborted
                        );
                        if (deleteFlag && !hasNewFile)
                        {
                            continue;
                        }
                    }

                    if (!hasNewFile)
                        continue; // skip jika tidak ada file baru

                    var baseName = SanitizeFileName($"{prefix}_R{round}_{vendor.VendorId}");

                    await using var stream = file!.OpenReadStream();
                    var uploadResult = await _procDocService.UploadAsync(
                        new UploadProcDocumentRequest
                        {
                            ProcurementId = vm.ProcurementId,
                            DocumentTypeId = docTypeId,
                            Content = stream,
                            Size = file.Length,
                            FileName = $"{baseName}.pdf",
                            ContentType = "application/pdf",
                            Description = $"{docLabel} Ronde {round} - Vendor {vendor.VendorId}",
                            UploadedByUserId = userId,
                            NowUtc = DateTime.UtcNow,
                        },
                        HttpContext.RequestAborted
                    );

                    var entity = new VendorRoundLetter
                    {
                        ProcurementId = vm.ProcurementId,
                        VendorId = vendor.VendorId,
                        Round = round,
                        LetterNumber = string.IsNullOrWhiteSpace(letter) ? null : letter.Trim(),
                        ProcDocumentId = uploadResult.ProcDocumentId,
                        ProfitLossId = profitLossId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = userId,
                    };
                    await _roundLetterRepository.AddOrUpdateAsync(entity);
                }
            }

            await _roundLetterRepository.SaveChangesAsync(HttpContext.RequestAborted);
        }

        private Dictionary<int, Dictionary<int, IFormFile>> BuildLetterFileLookup()
        {
            var lookup = new Dictionary<int, Dictionary<int, IFormFile>>();
            var files = Request?.Form?.Files;
            if (files == null || files.Count == 0)
                return lookup;

            foreach (var formFile in files)
            {
                if (formFile == null || string.IsNullOrWhiteSpace(formFile.Name))
                    continue;

                var match = LetterFileFieldRegex.Match(formFile.Name);
                if (!match.Success)
                    continue;

                if (!int.TryParse(match.Groups[1].Value, out var vendorIndex))
                    continue;

                if (!int.TryParse(match.Groups[2].Value, out var roundIndex))
                    continue;

                if (!lookup.TryGetValue(vendorIndex, out var roundMap))
                {
                    roundMap = new Dictionary<int, IFormFile>();
                    lookup[vendorIndex] = roundMap;
                }

                roundMap[roundIndex] = formFile;
            }

            return lookup;
        }

        private static List<IFormFile?> MergeLetterFiles(
            List<IFormFile?>? boundFiles,
            Dictionary<int, Dictionary<int, IFormFile>> lookup,
            int vendorIndex
        )
        {
            var files = boundFiles is { Count: > 0 }
                ? new List<IFormFile?>(boundFiles)
                : new List<IFormFile?>();

            if (!lookup.TryGetValue(vendorIndex, out var roundFiles) || roundFiles.Count == 0)
            {
                return files;
            }

            var requiredLength =
                roundFiles.Keys.Count > 0
                    ? Math.Max(files.Count, roundFiles.Keys.Max() + 1)
                    : files.Count;

            while (files.Count < requiredLength)
            {
                files.Add(null);
            }

            foreach (var kvp in roundFiles)
            {
                var roundIndex = kvp.Key;
                if (roundIndex < 0)
                    continue;

                if (roundIndex >= files.Count)
                {
                    while (files.Count <= roundIndex)
                    {
                        files.Add(null);
                    }
                }

                files[roundIndex] = kvp.Value;
            }

            return files;
        }

        private async Task RepopulateVendorChoices(ProfitLossInputViewModel viewModel)
        {
            var vendors = await _vendorService.GetAllVendorsAsync();
            viewModel.VendorChoices = vendors
                .Select(v => new VendorChoiceViewModel { Id = v.VendorId, Name = v.VendorName })
                .ToList();

            var procurement = await _procurementService.GetProcurementByIdAsync(
                viewModel.ProcurementId
            );

            // Build a lookup for existing Items to preserve Unit/UnitRevenue from form submission
            var existingItemsLookup =
                viewModel
                    .Items?.Where(i => !string.IsNullOrEmpty(i.ProcOfferId))
                    .ToDictionary(
                        i => i.ProcOfferId,
                        i => (Unit: i.UnitItems, UnitRevenue: i.UnitRevenue),
                        StringComparer.OrdinalIgnoreCase
                    ) ?? new Dictionary<string, (string? Unit, string? UnitRevenue)>();

            viewModel.OfferItems = (procurement?.ProcOffers ?? [])
                .Select(o =>
                {
                    // Try to get Unit/UnitRevenue from submitted form data first
                    existingItemsLookup.TryGetValue(o.ProcOfferId, out var existing);

                    return new ProcOfferLiteVm
                    {
                        ProcOfferId = o.ProcOfferId,
                        ItemPenawaran = o.ItemPenawaran,
                        Quantity = o.Qty,
                        Unit = existing.Unit ?? o.Unit,
                        UnitRevenue = existing.UnitRevenue ?? o.UnitRevenue,
                    };
                })
                .ToList();

            // Set ViewBag properties for the view
            ViewBag.ProcNum = procurement?.ProcNum;
            ViewBag.IssueDate = procurement?.CreatedAt.ToString("d MMMM yyyy");
            ViewBag.JobTypeName = procurement?.JobType?.TypeName ?? "Unknown";
            ViewBag.UnitTypes = await _unitTypeRepository.GetActiveAsync();

            // Backfill quantity ke Items (Billing) jika hilang
            if (viewModel.Items != null && viewModel.Items.Count > 0)
            {
                var qtyMap = viewModel.OfferItems.ToDictionary(
                    o => o.ProcOfferId,
                    o => o.Quantity,
                    StringComparer.OrdinalIgnoreCase
                );

                foreach (var item in viewModel.Items)
                {
                    if (item.Quantity <= 0 && qtyMap.TryGetValue(item.ProcOfferId, out var q))
                    {
                        item.Quantity = (int)q;
                    }
                }
            }
        }

        private void RemoveAutoGeneratedProcurementValidation()
        {
            var prefix = $"{nameof(ProcurementCreateViewModel.Procurement)}.";
            
            // Remove all auto-generated validation errors for Procurement model
            // We handle validation manually in ValidateProcurementForm()
            var fieldsToRemove = new[]
            {
                // Generated fields (auto-filled by system)
                nameof(Procurement.ProcNum),
                nameof(Procurement.ProcurementId),
                nameof(Procurement.PicOpsUserId),
                nameof(Procurement.UserId),
                nameof(Procurement.CreatedAt),
                nameof(Procurement.ProcurementStatus),
                
                // Date fields (handle with custom validation)
                nameof(Procurement.DocumentDate),
                nameof(Procurement.StartDate),
                nameof(Procurement.EndDate),
                
                // Enum fields (handle with custom validation)
                nameof(Procurement.ContractType),
                nameof(Procurement.ProcurementCategory),
                nameof(Procurement.ProjectRegion),
                
                // Required text fields (handle with custom validation)
                nameof(Procurement.JobName),
                nameof(Procurement.JobTypeId),
                
                // Approval user fields (optional for Draft, required for Create - handle manually)
                nameof(Procurement.AnalystHteUserId),
                nameof(Procurement.AssistantManagerUserId),
                nameof(Procurement.ManagerUserId),
                
                // Navigation properties (never from form)
                nameof(Procurement.Status),
                nameof(Procurement.JobType),
            };
            
            foreach (var field in fieldsToRemove)
            {
                ModelState.Remove($"{prefix}{field}");
            }
            
            // Also remove the base Procurement key if exists
            ModelState.Remove(nameof(ProcurementCreateViewModel.Procurement));
        }

        private void RemoveDetailsValidation()
        {
            var detailKeys = Request.Form["Details.Index"].ToArray();
            foreach (var key in detailKeys)
            {
                ModelState.Remove($"Details[{key}].ProcurementId");
                ModelState.Remove($"Details[{key}].Procurement");
                ModelState.Remove($"Details[{key}].ProcDetailId");
            }
        }

        private void RemoveOffersValidation()
        {
            var offerKeys = Request.Form["Offers.Index"].ToArray();
            foreach (var key in offerKeys)
            {
                ModelState.Remove($"Offers[{key}].ProcurementId");
                ModelState.Remove($"Offers[{key}].Procurement");
                ModelState.Remove($"Offers[{key}].ProcOfferId");
                // Remove auto-generated "The value '' is invalid" for numeric fields
                ModelState.Remove($"Offers[{key}].Qty");
                ModelState.Remove($"Offers[{key}].ItemPenawaran");
                ModelState.Remove($"Offers[{key}].Unit");
            }
        }

        private async Task<ProcurementCreateViewModel> BuildCreateViewModelAsync(
            string? jobTypeId,
            ProcurementCategory? category
        )
        {
            var (jobTypes, _) = await _procurementService.GetRelatedEntitiesForProcurementAsync();
            var selectedJobType =
                jobTypes.FirstOrDefault(j => j.JobTypeId == jobTypeId) ?? jobTypes.FirstOrDefault();

            var unitTypes = await _unitTypeRepository.GetActiveAsync();

            var viewModel = new ProcurementCreateViewModel
            {
                Procurement = new Procurement
                {
                    JobTypeId = selectedJobType?.JobTypeId!,
                    ProcurementCategory = category ?? ProcurementCategory.Barang,
                },
                Details = [],
                Offers = [],
                JobTypes = jobTypes,
                SelectedJobTypeName = selectedJobType?.TypeName ?? "Other",
            };

            await PopulateCreateUserSelectListsAsync(viewModel);

            ViewBag.UnitTypes = unitTypes;
            ViewBag.SelectedJobTypeName = selectedJobType?.TypeName ?? "Other";

            return viewModel;
        }

        private async Task RepopulateCreateViewModel(ProcurementCreateViewModel createViewModel)
        {
            ArgumentNullException.ThrowIfNull(createViewModel);
            createViewModel.Procurement ??= new Procurement();

            await PopulateCreateUserSelectListsAsync(createViewModel);

            var (jobTypes, _) = await _procurementService.GetRelatedEntitiesForProcurementAsync();
            createViewModel.JobTypes = jobTypes;

            if (string.IsNullOrWhiteSpace(createViewModel.Procurement.JobTypeId))
                createViewModel.Procurement.JobTypeId = jobTypes.FirstOrDefault()?.JobTypeId!;

            var jobTypeName = jobTypes
                .FirstOrDefault(w => w.JobTypeId == createViewModel.Procurement.JobTypeId)
                ?.TypeName;

            createViewModel.SelectedJobTypeName = jobTypeName ?? "Other";

            var unitTypes = await _unitTypeRepository.GetActiveAsync();
            ViewBag.UnitTypes = unitTypes;
            ViewBag.SelectedJobTypeName = createViewModel.SelectedJobTypeName;
        }

        private async Task PopulateCreateUserSelectListsAsync(ProcurementCreateViewModel viewModel)
        {
            viewModel.PicOpsUsers = await BuildUserSelectListAsync(
                "Operator",
                viewModel.Procurement.PicOpsUserId
            );
            viewModel.AnalystUsers = await BuildUserSelectListAsync(
                "Analyst HTE & LTS",
                viewModel.Procurement.AnalystHteUserId
            );
            viewModel.AssistantManagerUsers = await BuildUserSelectListAsync(
                "Assistant Manager HTE",
                viewModel.Procurement.AssistantManagerUserId
            );
            viewModel.ManagerUsers = await BuildUserSelectListAsync(
                "Manager Transport & Logistic",
                viewModel.Procurement.ManagerUserId
            );
        }

        private async Task<IEnumerable<SelectListItem>> BuildUserSelectListAsync(
            string roleName,
            string? selectedUserId
        )
        {
            if (_userManager == null)
                return Enumerable.Empty<SelectListItem>();

            var users = await _userManager.GetUsersInRoleAsync(roleName);

            return users
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .Select(user => new SelectListItem
                {
                    Value = user.Id,
                    Text = BuildUserDisplayName(user),
                    Selected = string.Equals(user.Id, selectedUserId, StringComparison.Ordinal),
                })
                .ToList();
        }

        private static string BuildUserDisplayName(User user)
        {
            var parts = new[] { user.FirstName, user.LastName }.Where(part =>
                !string.IsNullOrWhiteSpace(part)
            );
            var fullName = string.Join(' ', parts);
            return string.IsNullOrWhiteSpace(fullName)
                ? user.UserName ?? user.Email ?? "Unknown User"
                : fullName;
        }

        private async Task RepopulateEditViewModel(ProcurementEditViewModel viewModel)
        {
            var (jobTypes, statuses) =
                await _procurementService.GetRelatedEntitiesForProcurementAsync();
            viewModel.JobTypes = jobTypes;
            viewModel.Statuses = statuses;
            await PopulateEditUserSelectListsAsync(viewModel);

            var jobTypeName = jobTypes
                .FirstOrDefault(w => w.JobTypeId == viewModel.JobTypeId)
                ?.TypeName;
            ViewBag.SelectedJobTypeName = jobTypeName ?? "Other";
        }

        private async Task PopulateEditUserSelectListsAsync(ProcurementEditViewModel viewModel)
        {
            viewModel.PicOpsUsers = await BuildUserSelectListAsync(
                "Operator",
                viewModel.PicOpsUserId
            );
            viewModel.AnalystUsers = await BuildUserSelectListAsync(
                "Analyst HTE & LTS",
                viewModel.AnalystHteUserId
            );
            viewModel.AssistantManagerUsers = await BuildUserSelectListAsync(
                "Assistant Manager HTE",
                viewModel.AssistantManagerUserId
            );
            viewModel.ManagerUsers = await BuildUserSelectListAsync(
                "Manager Transport & Logistic",
                viewModel.ManagerUserId
            );
        }

        private async Task PopulateUserFullNamesAsync(Procurement procurement)
        {
            var map = await GetUserNamesAsync(
                new[]
                {
                    procurement.PicOpsUserId,
                    procurement.AnalystHteUserId,
                    procurement.AssistantManagerUserId,
                    procurement.ManagerUserId,
                }
            );

            string Resolve(string? id) => !string.IsNullOrEmpty(id) && map.TryGetValue(id, out var name) ? name : id ?? "-";

            ViewBag.PicOpsName = Resolve(procurement.PicOpsUserId);
            ViewBag.AnalystName = Resolve(procurement.AnalystHteUserId);
            ViewBag.AssistantManagerName = Resolve(procurement.AssistantManagerUserId);
            ViewBag.ManagerName = Resolve(procurement.ManagerUserId);
        }

        private async Task<Dictionary<string, string>> BuildUserNameMapAsync(
            IEnumerable<Procurement> procurements
        )
        {
            var ids = procurements
                .SelectMany(p =>
                    new[]
                    {
                        p.PicOpsUserId,
                        p.AnalystHteUserId,
                        p.AssistantManagerUserId,
                        p.ManagerUserId,
                    }
                )
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            return await GetUserNamesAsync(ids);
        }

        private async Task<Dictionary<string, string>> GetUserNamesAsync(IEnumerable<string?> ids)
        {
            var uniqueIds = ids.Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            var users = await _userManager
                .Users.Where(u => uniqueIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.UserName,
                    u.Email,
                })
                .ToListAsync();

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var user in users)
            {
                var name =
                    !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName
                    : !string.IsNullOrWhiteSpace(user.UserName) ? user.UserName!
                    : user.Email ?? user.Id;
                map[user.Id] = name;
            }

            foreach (var id in uniqueIds)
            {
                if (!map.ContainsKey(id))
                    map[id] = id;
            }

            return map;
        }

        /// <summary>
        /// Check if current user can edit procurement based on their role and procurement status.
        /// - Roles that can edit before pickup (Draft, Created): Admin, Operation, Assistant Manager HTE
        /// - Roles that can only edit after In Progress: AR, AP-Invoice, Analyst HTE & LTS, Supply Chain Management
        /// </summary>
        private bool CanUserEditProcurementByStatus(Procurement procurement)
        {
            var statusName = procurement.Status?.StatusName ?? "";
            
            // Status yang dianggap "sebelum pickup" - hanya Operation/Admin yang boleh edit
            var prePickupStatuses = new[] { "Draft", "Created", "Waiting Pickup" };
            var isPrePickup = prePickupStatuses.Contains(statusName, StringComparer.OrdinalIgnoreCase);
            
            // Roles yang boleh edit di tahap awal (sebelum/saat pickup)
            var earlyEditRoles = new[] { "Admin", "Operation", "Assistant Manager HTE" };
            
            // Roles yang hanya boleh edit setelah In Progress
            var lateEditRoles = new[] { "AR", "AP-Invoice", "Analyst HTE & LTS", "Supply Chain Management" };
            
            // Check user roles
            var userRoles = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            
            bool hasEarlyEditRole = userRoles.Any(r => earlyEditRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
            bool hasLateEditRole = userRoles.Any(r => lateEditRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
            
            // Admin dan earlyEditRoles bisa edit kapan saja
            if (hasEarlyEditRole)
                return true;
            
            // lateEditRoles hanya bisa edit jika status sudah melewati pickup (In Progress atau setelahnya)
            if (hasLateEditRole && isPrePickup)
                return false;
            
            // Default: allow jika punya permission Edit
            return true;
        }

        #endregion
    }
}
