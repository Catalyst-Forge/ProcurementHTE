using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;
using System.Security.Claims;

namespace ProcurementHTE.Web.Controllers.ProcurementModule
{
    [Authorize]
    public class ProcurementsController : Controller
    {
        #region Construct

        private const string ActivePageName = "Index Procurements";
        private readonly IProcurementService _procurementService;
        private readonly IVendorService _vendorService;
        private readonly IProfitLossService _pnlService;
        private readonly IVendorOfferService _voService;
        private readonly IDocumentGenerator _documentGenerator;
        private readonly IDocumentTypeService _docTypeService;
        private readonly IProcDocumentService _procDocService;
        private readonly ILogger<ProcurementsController> _logger;
        private readonly UserManager<User> _userManager;

        public ProcurementsController(
            IProcurementService procurementService,
            IVendorService vendorService,
            IProfitLossService pnlService,
            IVendorOfferService voService,
            IDocumentGenerator documentGenerator,
            IDocumentTypeService docTypeService,
            IProcDocumentService procDocService,
            ILogger<ProcurementsController> logger,
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
            _logger = logger;
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
                _logger.LogError(ex, "Error loading Procurement details for ID: {id}", id);
                TempData["ErrorMessage"] = "Failed to load procurement details: " + ex.Message;

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
                Details = [],
                Offers = [],
            };
            await PopulateCreateUserSelectListsAsync(createWoVM);
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
            //ModelState.Remove("Procurement.ProcNum");
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
                    ? "Procurement saved as draft successfully"
                    : "Procurement created successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save procurement");
                ModelState.AddModelError(
                    "",
                    "An error occurred while saving the data: " + ex.Message
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
                var (jobTypes, statuses) =
                    await _procurementService.GetRelatedEntitiesForProcurementAsync();

                var viewModel = new ProcurementEditViewModel
                {
                    ProcurementId = procurement.ProcurementId,
                    ProcNum = procurement.ProcNum,
                    JobTypeId = procurement.JobTypeId,
                    JobTypeOther = procurement.JobTypeOther,
                    ContractType = procurement.ContractType,
                    JobName = procurement.JobName,
                    SpkNumber = procurement.SpkNumber,
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
                ViewBag.CreatePartial = ResolveCreatePartialByName(jobTypeName);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Procurement for edit, ID: {id}", id);
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
                var procurement = new Procurement
                {
                    ProcurementId = editViewModel.ProcurementId,
                    ProcNum = editViewModel.ProcNum,
                    JobTypeId = editViewModel.JobTypeId,
                    JobTypeOther = editViewModel.JobTypeOther,
                    ContractType = editViewModel.ContractType,
                    JobName = editViewModel.JobName!,
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
                    $"An error occurred while updating the data: {ex.Message}"
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
            try
            {
                var procurement = await _procurementService.GetProcurementByIdAsync(id);
                if (procurement == null)
                    return NotFound();

                await _procurementService.DeleteProcurementAsync(procurement);
                TempData["SuccessMessage"] = "Procurement deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to delete procurement: {ex.Message}";
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
                            Quantity = o.Qty,
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
                        "No offer items (ProcOffer) exist. Add them on the Edit Procurement page to input prices per item.";

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
                    "Profit & Loss created successfully and documents were generated.";

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

                var vendors = await _vendorService.GetAllVendorsAsync();
                var procurement =
                    await _procurementService.GetProcurementByIdAsync(dto.ProcurementId)
                    ?? throw new KeyNotFoundException("Procurement tidak ditemukan");

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
                    Vendors = dtoVendors
                        .Select(v => new VendorItemOfferInputVm
                        {
                            VendorId = v.VendorId,
                            Items = (v.Items ?? [])
                                .Select(it => new VendorOfferPerItemInputVm
                                {
                                    ProcOfferId = it.ProcOfferId,
                                    Prices = it.Prices?.ToList() ?? [],
                                    Letters = it.Letters?.ToList() ?? [],
                                    Quantity = it.Quantity,
                                    Trip = it.Trip,
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
                _logger.LogInformation("? EditPnL view akan ditampilkan");
                return View("CreateProfitLoss", vm);
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
            TempData["SuccessMessage"] = "Profit & Loss updated successfully";
            return RedirectToAction(nameof(Details), new { id = viewModel.ProcurementId });
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
                        Letters = [],
                        Quantity = item.Quantity,
                        Trip = item.Trip,
                    };

                    var prices = item.Prices ?? [];
                    var letters = item.Letters ?? [];

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
            ArgumentNullException.ThrowIfNull(createViewModel);
            createViewModel.Procurement ??= new Procurement();

            await PopulateCreateUserSelectListsAsync(createViewModel);

            var jobTypeName = (
                await _procurementService.GetJobTypeByIdAsync(
                    createViewModel.Procurement.JobTypeId ?? string.Empty
                )
            )?.TypeName;

            ViewBag.CreatePartial = ResolveCreatePartialByName(jobTypeName);
            ViewBag.SelectedJobTypeName = jobTypeName ?? "Other";
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
            ViewBag.CreatePartial = ResolveCreatePartialByName(jobTypeName);
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
