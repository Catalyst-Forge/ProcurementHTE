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

        public ProcurementsController(
            IProcurementService procurementService,
            IVendorService vendorService,
            IProfitLossService pnlService,
            IVendorOfferService voService,
            IDocumentGenerator documentGenerator,
            IDocumentTypeService docTypeService,
            IProcDocumentService procDocService,
            IVendorRoundLetterRepository roundLetterRepository,
            UserManager<User> userManager
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
            RemoveAutoGeneratedProcurementValidation();
            RemoveDetailsValidation();
            RemoveOffersValidation();

            procurementViewModel.Procurement ??= new Procurement();
            procurementViewModel.Details ??= new List<ProcDetail>();
            procurementViewModel.Offers ??= new List<ProcOffer>();
            procurementViewModel.Procurement.UserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            // Validasi JobTypeId
            if (string.IsNullOrWhiteSpace(procurementViewModel.Procurement.JobTypeId))
            {
                ModelState.AddModelError(
                    $"{nameof(procurementViewModel.Procurement)}.{nameof(Procurement.JobTypeId)}",
                    "Pilih job type terlebih dahulu"
                );
            }

            // Validasi ContractType
            if (procurementViewModel.Procurement.ContractType == 0)
            {
                ModelState.AddModelError(
                    $"{nameof(procurementViewModel.Procurement)}.{nameof(Procurement.ContractType)}",
                    "Contract type wajib dipilih"
                );
            }

            // Validasi ProcurementCategory
            if (procurementViewModel.Procurement.ProcurementCategory == 0)
            {
                ModelState.AddModelError(
                    $"{nameof(procurementViewModel.Procurement)}.{nameof(Procurement.ProcurementCategory)}",
                    "Jenis pengadaan wajib dipilih"
                );
            }

            // Validasi JobName
            if (string.IsNullOrWhiteSpace(procurementViewModel.Procurement.JobName))
            {
                ModelState.AddModelError(
                    $"{nameof(procurementViewModel.Procurement)}.{nameof(Procurement.JobName)}",
                    "Nama pekerjaan wajib diisi"
                );
            }

            // Validasi StartDate dan EndDate
            if (procurementViewModel.Procurement.StartDate == default)
            {
                ModelState.AddModelError(
                    $"{nameof(procurementViewModel.Procurement)}.{nameof(Procurement.StartDate)}",
                    "Tanggal mulai wajib diisi"
                );
            }

            if (procurementViewModel.Procurement.EndDate == default)
            {
                ModelState.AddModelError(
                    $"{nameof(procurementViewModel.Procurement)}.{nameof(Procurement.EndDate)}",
                    "Tanggal selesai wajib diisi"
                );
            }

            if (
                procurementViewModel.Procurement.StartDate != default
                && procurementViewModel.Procurement.EndDate != default
                && procurementViewModel.Procurement.EndDate
                    < procurementViewModel.Procurement.StartDate
            )
            {
                ModelState.AddModelError(
                    $"{nameof(procurementViewModel.Procurement)}.{nameof(Procurement.EndDate)}",
                    "Tanggal selesai harus setelah tanggal mulai"
                );
            }

            // Validasi RaNumber untuk Jasa
            if (
                procurementViewModel.Procurement.ProcurementCategory == ProcurementCategory.Jasa
                && string.IsNullOrWhiteSpace(procurementViewModel.Procurement.RaNumber)
            )
            {
                ModelState.AddModelError(
                    $"{nameof(procurementViewModel.Procurement)}.{nameof(Procurement.RaNumber)}",
                    "RA Number wajib diisi untuk jenis pengadaan jasa"
                );
            }

            // Validasi Submit Action
            if (!string.IsNullOrWhiteSpace(submitAction))
            {
                var status = await _procurementService.GetStatusByNameAsync(submitAction);
                if (status == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Status '{submitAction}' tidak ditemukan. Pastikan entries di table Statuses ada."
                    );
                }
                else
                {
                    procurementViewModel.Procurement.StatusId = status.StatusId;
                }
            }
            else
            {
                ModelState.AddModelError("", "Aksi submit tidak dikenali");
            }

            // Validasi Offers (minimal 1 item jika bukan draft)
            if (
                submitAction?.Equals("Created", StringComparison.OrdinalIgnoreCase) == true
                && (procurementViewModel.Offers == null || procurementViewModel.Offers.Count == 0)
            )
            {
                ModelState.AddModelError(
                    nameof(procurementViewModel.Offers),
                    "Minimal harus ada 1 item penawaran untuk membuat procurement"
                );
            }

            if (!ModelState.IsValid)
            {
                await RepopulateCreateViewModel(procurementViewModel);
                return View("CreateByType", procurementViewModel);
            }

            try
            {
                await _procurementService.AddProcurementWithDetailsAsync(
                    procurementViewModel.Procurement,
                    procurementViewModel.Details!,
                    procurementViewModel.Offers!
                );
                TempData["SuccessMessage"] = submitAction!.Equals(
                    "Draft",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Procurement saved as draft successfully"
                    : "Procurement created successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", $"Validasi gagal: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(
                    "",
                    $"Gagal menyimpan data ke database: {ex.InnerException?.Message ?? ex.Message}"
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
                    $"Terjadi kesalahan saat menyimpan data: {ex.Message}"
                );
            }

            await RepopulateCreateViewModel(procurementViewModel);
            return View("CreateByType", procurementViewModel);
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

            try
            {
                var (jobTypes, statuses) =
                    await _procurementService.GetRelatedEntitiesForProcurementAsync();

                var viewModel = new ProcurementEditViewModel
                {
                    ProcurementId = procurement.ProcurementId,
                    ProcNum = procurement.ProcNum,
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
                    SpmpNumber = procurement.SpmpNumber,
                    MemoNumber = procurement.MemoNumber,
                    OeNumber = procurement.OeNumber,
                    RaNumber = procurement.RaNumber,
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

                var jobTypeName = procurement.JobType?.TypeName ?? "Other";
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
        public async Task<IActionResult> Edit(string id, ProcurementEditViewModel editViewModel)
        {
            if (id != editViewModel.ProcurementId)
            {
                ModelState.AddModelError("", "ID Procurement tidak sesuai");
                return NotFound();
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
            if (string.IsNullOrWhiteSpace(editViewModel.PicOpsUserId))
            {
                ModelState.AddModelError(
                    nameof(editViewModel.PicOpsUserId),
                    "PIC Operations wajib dipilih"
                );
            }

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

            if (!ModelState.IsValid)
            {
                await RepopulateEditViewModel(editViewModel);
                return View(editViewModel);
            }

            try
            {
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
                    SpmpNumber = editViewModel.SpmpNumber,
                    MemoNumber = editViewModel.MemoNumber,
                    OeNumber = editViewModel.OeNumber,
                    RaNumber = editViewModel.RaNumber,
                    ProjectCode = editViewModel.ProjectCode,
                    Wonum = editViewModel.Wonum,
                    LtcName = editViewModel.LtcName,
                    Note = editViewModel.Note,
                    ProcurementCategory = editViewModel.ProcurementCategory,
                    PicOpsUserId = editViewModel.PicOpsUserId!,
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

                // Cek apakah procurement memiliki data terkait yang harus dihapus dulu
                var documents = await _procDocService.ListByProcurementAsync(id);
                if (documents != null && documents.Count > 0)
                {
                    TempData["ErrorMessage"] =
                        $"Tidak dapat menghapus procurement karena masih memiliki {documents.Count} dokumen terkait. Hapus dokumen terlebih dahulu.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var profitLoss = await _pnlService.GetByProcurementAsync(id);
                if (profitLoss != null)
                {
                    TempData["ErrorMessage"] =
                        "Tidak dapat menghapus procurement karena masih memiliki data Profit & Loss terkait. Hapus Profit & Loss terlebih dahulu.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                await _procurementService.DeleteProcurementAsync(procurement);
                TempData["SuccessMessage"] = "Procurement deleted successfully!";
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
                    FileName = $"Profit_Loss_{procurement.ProcNum}.pdf",
                    ContentType = "application/pdf",
                    Bytes = pdfBytes,
                    Description = "Profit & Loss auto-generated",
                    GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    CreatedAt = DateTime.Now,
                };

                var saveResult = await _procDocService.SaveGeneratedAsync(generateReq);

                TempData["SuccessMessage"] =
                    "Profit & Loss created successfully and documents were generated.";

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

                var vendorModels = vendorLookup
                    .Select(kvp =>
                    {
                        var vendorEntries = kvp.Value;
                        var aggregatedItems = vendorEntries
                            .SelectMany(v => v.Items ?? [])
                            .GroupBy(it => it.ProcOfferId, StringComparer.OrdinalIgnoreCase)
                            .Select(group =>
                            {
                                var representative = group.Last();
                                return new VendorOfferPerItemInputVm
                                {
                                    ProcOfferId = representative.ProcOfferId,
                                    Prices = representative.Prices?.ToList() ?? [],
                                    Quantity = representative.Quantity,
                                    Trip = representative.Trip,
                                };
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

                var vm = new ProfitLossEditViewModel
                {
                    ProfitLossId = dto.ProfitLossId,
                    ProcurementId = dto.ProcurementId,
                    AccrualAmount = dto.AccrualAmount,
                    RealizationAmount = dto.RealizationAmount,
                    Distance = dto.Distance,
                    Items = dtoItems
                        .Select(x => new ItemTariffInputVm
                        {
                            ProcOfferId = x.ProcOfferId,
                            Quantity = x.Quantity,
                            TarifAwal = x.TarifAwal,
                            TarifAdd = x.TarifAdd,
                            KmPer25 = x.KmPer25,
                            OperatorCost = x.OperatorCost,
                        })
                        .ToList(),
                    Vendors = vendorModels,
                    SelectedVendorIds = selectedVendorIds,
                    VendorChoices = vendors
                        .Select(v => new VendorChoiceViewModel
                        {
                            Id = v.VendorId,
                            Name = v.VendorName,
                        })
                        .ToList(),
                    OfferItems = (procurement.ProcOffers ?? [])
                        .Select(x => new ProcOfferLiteVm
                        {
                            ProcOfferId = x.ProcOfferId,
                            ItemPenawaran = x.ItemPenawaran,
                        })
                        .ToList(),
                };

                ViewBag.ProcNum = procurement.ProcNum;
                ViewBag.IssueDate = procurement.CreatedAt.ToString("d MMMM yyyy");
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
                    Items = (viewModel.Items ?? [])
                        .Select(x => new ProfitLossItemInputDto
                        {
                            ProcOfferId = x.ProcOfferId,
                            Quantity = x.Quantity,
                            TarifAwal = x.TarifAwal ?? 0m,
                            TarifAdd = x.TarifAdd ?? 0m,
                            KmPer25 = x.KmPer25 ?? 0m,
                            OperatorCost = x.OperatorCost ?? 0m,
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
                TempData["SuccessMessage"] = "Profit & Loss updated successfully";
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

        #region Helper Methods

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
            var items = (vm.Items ?? [])
                .Select(x => new ProfitLossItemInputDto
                {
                    ProcOfferId = x.ProcOfferId,
                    Quantity = x.Quantity,
                    TarifAwal = x.TarifAwal ?? 0m,
                    TarifAdd = x.TarifAdd ?? 0m,
                    KmPer25 = x.KmPer25 ?? 0m,
                    OperatorCost = x.OperatorCost ?? 0m,
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
                            await _procDocService.DeleteAsync(docId!);
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
            viewModel.OfferItems = (procurement?.ProcOffers ?? [])
                .Select(o => new ProcOfferLiteVm
                {
                    ProcOfferId = o.ProcOfferId,
                    ItemPenawaran = o.ItemPenawaran,
                    Quantity = o.Qty,
                })
                .ToList();

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
            ModelState.Remove($"{prefix}{nameof(Procurement.ProcNum)}");
            ModelState.Remove($"{prefix}{nameof(Procurement.ProcurementId)}");
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
            ViewBag.SelectedJobTypeName = createViewModel.SelectedJobTypeName;
        }

        private async Task PopulateCreateUserSelectListsAsync(ProcurementCreateViewModel viewModel)
        {
            viewModel.PicOpsUsers = await BuildUserSelectListAsync(
                "Operation",
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
                "Operation",
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

            string Resolve(string id) => map.TryGetValue(id, out var name) ? name : id;

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

        #endregion
    }
}
