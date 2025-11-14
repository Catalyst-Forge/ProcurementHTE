using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers
{
    [Authorize]
    public class ProcurementsController(
        IProcurementService procurementService,
        IVendorService vendorService,
        IProfitLossService pnlService,
        IVendorOfferService voService,
        IDocumentGenerator documentGenerator,
        IDocumentTypeService docTypeService,
        IProcDocumentService procDocService,
        ILogger<ProcurementsController> logger
    ) : Controller
    {
        private readonly IProcurementService _procurementService = procurementService;
        private readonly IVendorService _vendorService = vendorService;
        private readonly IProfitLossService _pnlService = pnlService;
        private readonly IVendorOfferService _voService = voService;
        private readonly IDocumentGenerator _documentGenerator = documentGenerator;
        private readonly IDocumentTypeService _docTypeService = docTypeService;
        private readonly IProcDocumentService _procDocService = procDocService;
        private readonly ILogger<ProcurementsController> _logger = logger;

        #region CRUD Operations

        // GET: Procurements
        [Authorize(Policy = Permissions.Procurement.Read)]
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

            var procurements = await _procurementService.GetAllProcurementWithDetailsAsync(
                page,
                pageSize,
                search,
                selectedFields,
                ct
            );
            ViewBag.RouteData = new RouteValueDictionary
            {
                ["ActivePage"] = "Index Procurements",
                ["search"] = search,
                ["fields"] = string.Join(',', selectedFields),
                ["pageSize"] = pageSize,
            };

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
                _logger.LogError(ex, "Error loading Procurement details for ID: {id}", id);
                TempData["ErrorMessage"] = "Gagal memuat detail Procurement: " + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Procurements/SelectType
        [Authorize(Policy = Permissions.Procurement.Create)]
        public async Task<IActionResult> SelectType()
        {
            var related = await _procurementService.GetRelatedEntitiesForProcurementAsync();
            return View(related.JobTypes);
        }

        // POST: Procurements/SelectType
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Procurement.Create)]
        public IActionResult SelectType(string jobTypeId)
        {
            return RedirectToAction(nameof(CreateByType), new { jobTypeId });
        }

        // GET: Procurements/CreateByType?jobTypeId/1
        [HttpGet]
        //[Authorize(Policy = Permissions.Procurement.Create)]
        public async Task<IActionResult> CreateByType(string jobTypeId)
        {
            var jobType = await _procurementService.GetJobTypeByIdAsync(jobTypeId);
            if (jobType is null)
            {
                TempData["ErrorMessage"] = "Tipe Procurement tidak ditemukan";
                return RedirectToAction(nameof(SelectType));
            }

            ViewBag.SelectedJobTypeName = jobType.TypeName;

            var createWoVM = new ProcurementCreateViewModel
            {
                Procurement = new Procurement { JobTypeId = jobTypeId },
                Details = new List<ProcDetail>(),
                Offers = new List<ProcOffer>(),
            };
            ViewBag.CreatePartial = ResolveCreatePartialByName(jobType.TypeName);
            return View("CreateByType", createWoVM);
        }

        // POST: Procurements/CreateByType?jobTypeId/1
        // Post form Procurement
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Procurement.Create)]
        public async Task<IActionResult> Create(
            [FromForm] ProcurementCreateViewModel woViewModel,
            string submitAction
        )
        {
            //ModelState.Remove(nameof(User));
            ModelState.Remove("Procurement.ProcNum");
            //ModelState.Remove("Procurement.ProcurementId");

            RemoveDetailsValidation();
            RemoveOffersValidation();

            //woViewModel ??= new ProcurementCreateViewModel();
            //woViewModel.Details ??= new List<ProcDetail>();
            //woViewModel.Offers ??= new List<ProcOffer>();
            woViewModel.Procurement.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
                    woViewModel.Procurement.StatusId = status.StatusId;
                }
            }
            else
            {
                ModelState.AddModelError("", "Aksi submit tidak dikenali");
            }

            if (!ModelState.IsValid)
            {
                await RepopulateCreateViewModel(woViewModel);
                return View("CreateByType", woViewModel);
            }

            try
            {
                await _procurementService.AddProcurementWithDetailsAsync(
                    woViewModel.Procurement,
                    woViewModel.Details!,
                    woViewModel.Offers
                );
                TempData["SuccessMessage"] = submitAction.Equals(
                    "Draft",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Procurement berhasil disimpan sebagai draft"
                    : "Procurement berhasil dibuat";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menyimpan Procurement");
                ModelState.AddModelError(
                    "",
                    "Terjadi kesalahan saat menyimpan data: " + ex.Message
                );
            }
            await RepopulateCreateViewModel(woViewModel);

            return View("CreateByType", woViewModel);
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
                var (jobTypes, statuses) = await _procurementService.GetRelatedEntitiesForProcurementAsync();

                var viewModel = new ProcurementEditViewModel
                {
                    ProcurementId = procurement.ProcurementId,
                    ProcNum = procurement.ProcNum,
                    JobTypeId = procurement.JobTypeId,
                    JobType = procurement.JobType,
                    JobTypeOther = procurement.JobTypeOther,
                    ContractType = procurement.ContractType,
                    JobName = procurement.JobName,
                    SpkNumber = procurement.SpkNumber,
                    StartDate = procurement.StartDate,
                    EndDate = procurement.EndDate,
                    ProjectRegion = procurement.ProjectRegion,
                    DistanceKm = procurement.DistanceKm,
                    AccrualAmount = procurement.AccrualAmount,
                    RealizationAmount = procurement.RealizationAmount,
                    PotentialAccrualDate = procurement.PotentialAccrualDate,
                    SpmpNumber = procurement.SpmpNumber,
                    MemoNumber = procurement.MemoNumber,
                    OeNumber = procurement.OeNumber,
                    SelectedVendorName = procurement.SelectedVendorName,
                    VendorSphNumber = procurement.VendorSphNumber,
                    RaNumber = procurement.RaNumber,
                    ProjectCode = procurement.ProjectCode,
                    LtcName = procurement.LtcName,
                    Note = procurement.Note,
                    StatusId = procurement.StatusId,
                    PicOpsUserId = procurement.PicOpsUserId,
                    AnalystHteSignerUserId = procurement.AnalystHteSignerUserId,
                    AssistantManagerSignerUserId = procurement.AssistantManagerSignerUserId,
                    ManagerSignerUserId = procurement.ManagerSignerUserId,
                    Details = procurement.ProcDetails?.ToList() ?? new List<ProcDetail>(),
                    Offers = procurement.ProcOffers?.ToList() ?? new List<ProcOffer>(),
                    //JobTypes = jobTypes,
                    //Statuses = statuses,
                };

                var jobTypeName = procurement.JobTypeConfig?.TypeName ?? procurement.JobType ?? "Other";
                ViewBag.SelectedJobTypeName = jobTypeName;
                ViewBag.CreatePartial = ResolveCreatePartialByName(jobTypeName);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Procurement for edit, ID: {id}", id);
                TempData["ErrorMessage"] = $"Gagal memuat data untuk diedit: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Procurements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Procurement.Edit)]
        public async Task<IActionResult> Edit(string id, ProcurementEditViewModel editViewModel)
        {
            //ModelState.Remove(nameof(Procurement.User));

            if (id != editViewModel.ProcurementId)
                return NotFound();

            RemoveDetailsValidation();
            RemoveOffersValidation();

            if (!ModelState.IsValid)
            {
                await RepopulateEditViewModel(editViewModel);
                return View(editViewModel);
            }

            try
            {
var procurement = new Procurement {
    ProcurementId = editViewModel.ProcurementId,
    ProcNum = editViewModel.ProcNum,
    JobTypeId = editViewModel.JobTypeId,
    JobType = editViewModel.JobType,
    JobTypeOther = editViewModel.JobTypeOther,
    ContractType = editViewModel.ContractType,
    JobName = editViewModel.JobName,
    StartDate = editViewModel.StartDate,
    EndDate = editViewModel.EndDate,
    ProjectRegion = editViewModel.ProjectRegion,
    DistanceKm = editViewModel.DistanceKm,
    AccrualAmount = editViewModel.AccrualAmount,
    RealizationAmount = editViewModel.RealizationAmount,
    PotentialAccrualDate = editViewModel.PotentialAccrualDate,
    SpkNumber = editViewModel.SpkNumber,
    SpmpNumber = editViewModel.SpmpNumber,
    MemoNumber = editViewModel.MemoNumber,
    OeNumber = editViewModel.OeNumber,
    SelectedVendorName = editViewModel.SelectedVendorName,
    VendorSphNumber = editViewModel.VendorSphNumber,
    RaNumber = editViewModel.RaNumber,
    ProjectCode = editViewModel.ProjectCode,
    LtcName = editViewModel.LtcName,
    Note = editViewModel.Note,
    PicOpsUserId = editViewModel.PicOpsUserId,
    AnalystHteSignerUserId = editViewModel.AnalystHteSignerUserId,
    AssistantManagerSignerUserId = editViewModel.AssistantManagerSignerUserId,
    ManagerSignerUserId = editViewModel.ManagerSignerUserId,
    StatusId = editViewModel.StatusId
};


                await _procurementService.EditProcurementAsync(
                    procurement,
                    id,
                    editViewModel.Details ?? new List<ProcDetail>(),
                    editViewModel.Offers ?? new List<ProcOffer>()
                );
                TempData["SuccessMessage"] = "Procurement berhasil diupdate!";

                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Procurement not found for edit, ID: {id}", id);
                TempData["ErrorMessage"] = ex.Message;
                ModelState.AddModelError("", ex.Message);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Procurement, ID: {id}", id);
                ModelState.AddModelError(
                    "",
                    $"Terjadi kesalahan saat mengupdate data: {ex.Message}"
                );
                await RepopulateEditViewModel(editViewModel);
                return View(editViewModel);
            }
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
                _logger.LogError(ex, "Error loading Procurement for delete, ID: {id}", id);
                TempData["ErrorMessage"] = $"Gagal memuat data untuk delete: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Procurements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Procurement.Delete)]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var procurement = await _procurementService.GetProcurementByIdAsync(id);
                if (procurement == null)
                    return NotFound();

                await _procurementService.DeleteProcurementAsync(procurement);
                TempData["SuccessMessage"] = "Procurement berhasil dihapus!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal menghapus Procurement: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
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
                if (string.IsNullOrWhiteSpace(procurementId) || procurement == null)
                {
                    TempData["ErrorMessage"] = "Procurement tidak ditemukan";
                    return RedirectToAction("Index", "Procurement");
                }

                var vendors = await _vendorService.GetAllVendorsAsync();
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
                        })
                        .ToList(),
                };

                viewModel.Items = viewModel
                    .OfferItems.Select(o => new ItemTariffInputVm
                    {
                        ProcOfferId = o.ProcOfferId,
                        TarifAwal = 0,
                        TarifAdd = 0,
                        KmPer25 = 0,
                        OperatorCost = 0m,
                    })
                    .ToList();

                ViewBag.ProcNum = procurement.ProcNum;
                ViewBag.IssueDate = procurement.CreatedAt.ToString("d MMMM yyyy");

                if (viewModel.OfferItems.Count == 0)
                    TempData["WarningMessage"] =
                        "Belum ada Item Penawaran (ProcOffer). Tambahkan di halaman Edit Procurement agar bisa input harga per item.";

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating ProfitLoss form for Procurement: {ProcurementId}",
                    procurementId
                );
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

            if (!ModelState.IsValid)
            {
                await RepopulateVendorChoices(viewModel);
                LogModelStateErrors();

                return View("CreateProfitLoss", viewModel);
            }

            var selectedVendors = viewModel.SelectedVendorIds?.Distinct().ToList() ?? [];
            if (selectedVendors.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(viewModel.SelectedVendorIds),
                    "Pilih minimal 1 vendor"
                );
                await RepopulateVendorChoices(viewModel);

                return View(viewModel);
            }

            try
            {
                foreach (var key in Request.Form.Keys)
                {
                    _logger.LogWarning("FORM {Key} = {Val}", key, Request.Form[key]);
                }

                var dto = MapToDto(viewModel, selectedVendors);
                var pnl = await _pnlService.SaveInputAndCalculateAsync(dto);

                var wo =
                    await _procurementService.GetProcurementByIdAsync(dto.ProcurementId)
                    ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

                var pdfBytes = await _documentGenerator.GenerateProfitLossAsync(wo);

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
                    FileName = $"Profit_Loss_{wo.ProcNum}.pdf",
                    ContentType = "application/pdf",
                    Bytes = pdfBytes,
                    Description = "Profit & Loss auto-generated",
                    GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    CreatedAt = DateTime.Now,
                };

                var saveResult = await _procDocService.SaveGeneratedAsync(generateReq);

                TempData["SuccessMessage"] =
                    "Profit & Loss berhasil dibuat & generate dokumen telah berhasil";

                return RedirectToAction(nameof(Details), new { id = dto.ProcurementId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating ProfitLoss for Procurement: {ProcurementId}",
                    viewModel.ProcurementId
                );
                ModelState.AddModelError("", $"Error: {ex.Message}");
            }

            await RepopulateVendorChoices(viewModel);
            return View("CreateProfitLoss", viewModel);
        }

        private static bool IsWkhtmltoxMissing(Exception ex)
        {
            if (ex is DllNotFoundException)
                return true;

            if (
                !string.IsNullOrWhiteSpace(ex.Message)
                && ex.Message.Contains("libwkhtmltox", StringComparison.OrdinalIgnoreCase)
            )
                return true;

            return ex.InnerException != null && IsWkhtmltoxMissing(ex.InnerException);
        }

        // GET: /Procurements/EditPnL/5
        [HttpGet]
        public async Task<IActionResult> EditProfitLoss(string id)
        {
            _logger.LogInformation("?? EditPnL (GET) called with ID: {Id}", id);

            try
            {
                var dto = await _pnlService.GetEditDataAsync(id);
                var dtoItems = dto.Items ?? [];
                var dtoVendors = dto.Vendors ?? [];
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "EditPnL DTO snapshot for {ProfitLossId}: Items={ItemCount}, Vendors={VendorCount}",
                        dto.ProfitLossId,
                        dto.Items?.Count ?? 0,
                        dto.Vendors?.Count ?? 0
                    );
                    if (dto.Items != null)
                    {
                        var index = 0;
                        foreach (var item in dto.Items)
                        {
                            _logger.LogDebug(
                                "EditPnL Item[{Index}] ProcOfferId={ProcOfferId} TarifAwal={TarifAwal} TarifAdd={TarifAdd} KmPer25={KmPer25}",
                                index++,
                                item.ProcOfferId,
                                item.TarifAwal,
                                item.TarifAdd,
                                item.KmPer25
                            );
                        }
                    }
                }
                var vendors = await _vendorService.GetAllVendorsAsync();
                var wo =
                    await _procurementService.GetProcurementByIdAsync(dto.ProcurementId)
                    ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

                var vm = new ProfitLossEditViewModel
                {
                    ProfitLossId = dto.ProfitLossId,
                    ProcurementId = dto.ProcurementId,
                    // agregat lama (TarifAwal/TarifAdd/KmPer25) TIDAK dipakai lagi
                    Items = dtoItems
                        .Select(x => new ItemTariffInputVm
                        {
                            ProcOfferId = x.ProcOfferId,
                            TarifAwal = x.TarifAwal,
                            TarifAdd = x.TarifAdd,
                            KmPer25 = x.KmPer25,
                            OperatorCost = x.OperatorCost,
                        })
                        .ToList(),
                    Vendors = dtoVendors
                        .Select(v => new VendorItemOfferInputVm
                        {
                            VendorId = v.VendorId,
                            Items = (v.Items ?? new List<VendorOfferPerItemDto>())
                                .Select(it => new VendorOfferPerItemInputVm
                                {
                                    ProcOfferId = it.ProcOfferId,
                                    Prices = it.Prices?.ToList() ?? new List<decimal>(),
                                    Letters = it.Letters?.ToList() ?? new List<string>(),
                                })
                                .ToList(),
                        })
                        .ToList(),
                    SelectedVendorIds = dto.SelectedVendorIds.ToList(),
                    VendorChoices = vendors
                        .Select(v => new VendorChoiceViewModel
                        {
                            Id = v.VendorId,
                            Name = v.VendorName,
                        })
                        .ToList(),
                    OfferItems = (wo.ProcOffers ?? [])
                        .Select(x => new ProcOfferLiteVm
                        {
                            ProcOfferId = x.ProcOfferId,
                            ItemPenawaran = x.ItemPenawaran,
                        })
                        .ToList(),
                };

                ViewBag.ProcNum = wo.ProcNum;
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
                _logger.LogInformation("? EditPnL view akan ditampilkan");
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error di EditPnL");
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfitLoss(ProfitLossEditViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var vendors = await _vendorService.GetAllVendorsAsync();
                var wo = await _procurementService.GetProcurementByIdAsync(viewModel.ProcurementId);
                viewModel.VendorChoices = vendors
                    .Select(v => new VendorChoiceViewModel { Id = v.VendorId, Name = v.VendorName })
                    .ToList();
                viewModel.OfferItems = (wo?.ProcOffers ?? [])
                    .Select(x => new ProcOfferLiteVm
                    {
                        ProcOfferId = x.ProcOfferId,
                        ItemPenawaran = x.ItemPenawaran,
                    })
                    .ToList();
                return View(viewModel);
            }

            var distinctSelectedVendors = (viewModel.SelectedVendorIds ?? [])
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var update = new ProfitLossUpdateDto
            {
                ProfitLossId = viewModel.ProfitLossId,
                ProcurementId = viewModel.ProcurementId,
                Items = (viewModel.Items ?? [])
                    .Select(x => new ProfitLossItemInputDto
                    {
                        ProcOfferId = x.ProcOfferId,
                        TarifAwal = x.TarifAwal,
                        TarifAdd = x.TarifAdd,
                        KmPer25 = x.KmPer25,
                        OperatorCost = x.OperatorCost,
                    })
                    .ToList(),
                SelectedVendorIds = distinctSelectedVendors,
            };

            var allowedVendorSet = new HashSet<string>(
                distinctSelectedVendors,
                StringComparer.OrdinalIgnoreCase
            );
            update.Vendors = BuildVendorOfferDtos(viewModel.Vendors, allowedVendorSet);

            await _pnlService.EditProfitLossAsync(update);
            TempData["Success"] = "Profit & Loss berhasil diupdate";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Helper Methods

        private static string ResolveCreatePartialByName(string? typeName)
        {
            var key = (typeName ?? "Other").Trim().ToLowerInvariant();
            return key switch
            {
                "standby" or "stand by" or "stand_by" => "_CreateStandBy",
                "movingmobilization"
                or "moving mobilization"
                or "moving_mobilization"
                or "moving & mobilization"
                or "moving and mobilization"
                or "moving dan mobilization" => "_CreateMovingMobilization",
                "spotangkutan"
                or "spot angkutan"
                or "spot_angkutan"
                or "spot & angkutan"
                or "spot and angkutan"
                or "spot dan angkutan" => "_CreateSpotAngkutan",
                _ => "_CreateOther",
            };
        }

        // ProcurementsController.cs
        private static ProfitLossSummaryViewModel MapToViewModel(ProfitLossSummaryDto dto)
        {
            return new ProfitLossSummaryViewModel
            {
                ProfitLossId = dto.ProfitLossId,
                ProcurementId = dto.ProcurementId,

                TotalOperatorCost = dto.TotalOperatorCost,
                TotalRevenue = dto.TotalRevenue,

                SelectedVendorId = dto.SelectedVendorId,
                SelectedVendorName = dto.SelectedVendorName,
                SelectedFinalOffer = dto.SelectedFinalOffer,
                Profit = dto.Profit,
                ProfitPercent = dto.ProfitPercent,

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
                    TarifAwal = x.TarifAwal,
                    TarifAdd = x.TarifAdd,
                    KmPer25 = x.KmPer25,
                    OperatorCost = x.OperatorCost,
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
                        Prices = [],
                        Letters = [],
                    };

                    var prices = item.Prices ?? new List<decimal>();
                    var letters = item.Letters ?? new List<string>();

                    for (int idx = 0; idx < prices.Count; idx++)
                    {
                        var price = prices[idx];
                        if (price <= 0m)
                            continue;
                        dto.Prices.Add(price);
                        dto.Letters.Add(
                            idx < letters.Count
                                ? letters[idx]?.Trim() ?? string.Empty
                                : string.Empty
                        );
                    }

                    if (dto.Prices.Count > 0)
                        dtoItems.Add(dto);
                }

                if (dtoItems.Count > 0)
                {
                    result.Add(new VendorItemOffersDto { VendorId = vendorId, Items = dtoItems });
                }
            }

            return result;
        }

        private async Task RepopulateVendorChoices(ProfitLossInputViewModel viewModel)
        {
            var vendors = await _vendorService.GetAllVendorsAsync();
            viewModel.VendorChoices = vendors
                .Select(v => new VendorChoiceViewModel { Id = v.VendorId, Name = v.VendorName })
                .ToList();

            var wo = await _procurementService.GetProcurementByIdAsync(viewModel.ProcurementId);
            viewModel.OfferItems = (wo?.ProcOffers ?? [])
                .Select(o => new ProcOfferLiteVm
                {
                    ProcOfferId = o.ProcOfferId,
                    ItemPenawaran = o.ItemPenawaran,
                })
                .ToList();
        }

        private void LogModelStateErrors()
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                _logger.LogWarning("ModelState Error: {Error}", error.ErrorMessage);
            }
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

        private async Task RepopulateCreateViewModel(ProcurementCreateViewModel createViewModel)
        {
            var jobTypeName = (
                await _procurementService.GetJobTypeByIdAsync(
                    createViewModel.Procurement.JobTypeId ?? string.Empty
                )
            )?.TypeName;

            ViewBag.CreatePartial = ResolveCreatePartialByName(jobTypeName);
            ViewBag.SelectedJobTypeName = jobTypeName ?? "Other";
        }

        private async Task RepopulateEditViewModel(ProcurementEditViewModel viewModel)
        {
            var (jobTypes, statuses) = await _procurementService.GetRelatedEntitiesForProcurementAsync();
            viewModel.JobTypes = jobTypes;
            viewModel.Statuses = statuses;

            var jobTypeName = jobTypes
                .FirstOrDefault(w => w.JobTypeId == viewModel.JobTypeId)
                ?.TypeName;
            ViewBag.CreatePartial = ResolveCreatePartialByName(jobTypeName);
            ViewBag.SelectedJobTypeName = jobTypeName ?? "Other";
        }

        #endregion
    }
}
