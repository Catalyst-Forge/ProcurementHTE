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
    public class WorkOrdersController(
        IWorkOrderService woService,
        IVendorService vendorService,
        IProfitLossService pnlService,
        IVendorOfferService voService,
        IDocumentGenerator documentGenerator,
        IDocumentTypeService docTypeService,
        IWoDocumentService woDocService,
        ILogger<WorkOrdersController> logger
    ) : Controller
    {
        private readonly IWorkOrderService _woService = woService;
        private readonly IVendorService _vendorService = vendorService;
        private readonly IProfitLossService _pnlService = pnlService;
        private readonly IVendorOfferService _voService = voService;
        private readonly IDocumentGenerator _documentGenerator = documentGenerator;
        private readonly IDocumentTypeService _docTypeService = docTypeService;
        private readonly IWoDocumentService _woDocService = woDocService;
        private readonly ILogger<WorkOrdersController> _logger = logger;

        #region CRUD Operations

        // GET: WorkOrders
        [Authorize(Policy = Permissions.WO.Read)]
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

            var selectedFields = (fields ?? "WoNum, Description")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var workOrders = await _woService.GetAllWorkOrderWithDetailsAsync(
                page,
                pageSize,
                search,
                selectedFields,
                ct
            );
            ViewBag.RouteData = new RouteValueDictionary
            {
                ["ActivePage"] = "Index Work Orders",
                ["search"] = search,
                ["fields"] = string.Join(',', selectedFields),
                ["pageSize"] = pageSize,
            };

            return View(workOrders);
        }

        // GET: WorkOrders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(id);
                if (workOrder == null)
                    return NotFound();

                var summary = await _pnlService.GetSummaryByWorkOrderAsync(id);
                if (summary != null)
                {
                    var viewModel = MapToViewModel(summary);
                    ViewBag.SelectedVendorNames = summary.SelectedVendorNames;
                    ViewBag.PnlViewModel = viewModel;
                }

                var documents = await _woDocService.ListByWorkOrderAsync(id);
                if (documents != null)
                {
                    ViewBag.Documents = documents;
                }

                return View(workOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading WorkOrder details for ID: {id}", id);
                TempData["ErrorMessage"] = "Gagal memuat detail Work Order: " + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        // GET: WorkOrders/SelectType
        [Authorize(Policy = Permissions.WO.Create)]
        public async Task<IActionResult> SelectType()
        {
            var related = await _woService.GetRelatedEntitiesForWorkOrderAsync();
            return View(related.WoTypes);
        }

        // POST: WorkOrders/SelectType
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.WO.Create)]
        public IActionResult SelectType(string woTypeId)
        {
            return RedirectToAction(nameof(CreateByType), new { woTypeId });
        }

        // GET: WorkOrders/CreateByType?woTypeId/1
        [HttpGet]
        //[Authorize(Policy = Permissions.WO.Create)]
        public async Task<IActionResult> CreateByType(string woTypeId)
        {
            var woType = await _woService.GetWoTypeByIdAsync(woTypeId);
            if (woType is null)
            {
                TempData["ErrorMessage"] = "Tipe Work Order tidak ditemukan";
                return RedirectToAction(nameof(SelectType));
            }

            ViewBag.SelectedWoTypeName = woType.TypeName;

            var createWoVM = new WorkOrderCreateViewModel
            {
                WorkOrder = new WorkOrder { WoTypeId = woTypeId },
                Details = new List<WoDetail>(),
                Offers = new List<WoOffer>(),
            };
            ViewBag.CreatePartial = ResolveCreatePartialByName(woType.TypeName);
            return View("CreateByType", createWoVM);
        }

        // POST: WorkOrders/CreateByType?woTypeId/1
        // Post form work order
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.WO.Create)]
        public async Task<IActionResult> Create(
            [FromForm] WorkOrderCreateViewModel woViewModel,
            string submitAction
        )
        {
            //ModelState.Remove(nameof(User));
            ModelState.Remove("WorkOrder.WoNum");
            //ModelState.Remove("WorkOrder.WorkOrderId");

            RemoveDetailsValidation();
            RemoveOffersValidation();

            //woViewModel ??= new WorkOrderCreateViewModel();
            //woViewModel.Details ??= new List<WoDetail>();
            //woViewModel.Offers ??= new List<WoOffer>();
            woViewModel.WorkOrder.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(submitAction))
            {
                var status = await _woService.GetStatusByNameAsync(submitAction);
                if (status == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Status '{submitAction}' tidak ditemukan. Pastikan entries di table Statuses ada."
                    );
                }
                else
                {
                    woViewModel.WorkOrder.StatusId = status.StatusId;
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
                await _woService.AddWorkOrderWithDetailsAsync(
                    woViewModel.WorkOrder,
                    woViewModel.Details!,
                    woViewModel.Offers
                );
                TempData["SuccessMessage"] = submitAction.Equals(
                    "Draft",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Work Order berhasil disimpan sebagai draft"
                    : "Work Order berhasil dibuat";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menyimpan Work Order");
                ModelState.AddModelError(
                    "",
                    "Terjadi kesalahan saat menyimpan data: " + ex.Message
                );
            }
            await RepopulateCreateViewModel(woViewModel);

            return View("CreateByType", woViewModel);
        }

        // GET: WorkOrders/Edit/5
        [Authorize(Policy = Permissions.WO.Edit)]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var workOrder = await _woService.GetWorkOrderByIdAsync(id);
            if (workOrder == null)
                return NotFound();

            try
            {
                //var (woTypes, statuses) = await _woService.GetRelatedEntitiesForWorkOrderAsync();
                var viewModel = new WorkOrderEditViewModel
                {
                    WorkOrderId = workOrder.WorkOrderId,
                    WoNum = workOrder.WoNum,
                    Description = workOrder.Description,
                    Note = workOrder.Note,
                    WoTypeId = workOrder.WoTypeId,
                    ProcurementType = workOrder.ProcurementType,
                    WoNumLetter = workOrder.WoNumLetter,
                    DateLetter = workOrder.DateLetter,
                    From = workOrder.From,
                    To = workOrder.To,
                    WorkOrderLetter = workOrder.WorkOrderLetter,
                    WBS = workOrder.WBS,
                    GlAccount = workOrder.GlAccount,
                    DateRequired = workOrder.DateRequired,
                    Requester = workOrder.Requester,
                    Approved = workOrder.Approved,
                    XS1 = workOrder.XS1,
                    XS2 = workOrder.XS2,
                    XS3 = workOrder.XS3,
                    XS4 = workOrder.XS4,
                    StatusId = workOrder.StatusId,
                    Details = workOrder.WoDetails?.ToList() ?? new List<WoDetail>(),
                    Offers = workOrder.WoOffers?.ToList() ?? new List<WoOffer>(),
                    //WoTypes = woTypes,
                    //Statuses = statuses,
                };

                ViewData["EnumProcurementTypes"] = Enum.GetValues<ProcurementType>();
                ViewBag.SelectedWoTypeName = workOrder.WoType!.TypeName ?? "Other";
                ViewBag.CreatePartial = ResolveCreatePartialByName(workOrder.WoType!.TypeName);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Work Order for edit, ID: {id}", id);
                TempData["ErrorMessage"] = $"Gagal memuat data untuk diedit: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.WO.Edit)]
        public async Task<IActionResult> Edit(string id, WorkOrderEditViewModel editViewModel)
        {
            //ModelState.Remove(nameof(WorkOrder.User));

            if (id != editViewModel.WorkOrderId)
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
                var workOrder = new WorkOrder
                {
                    WorkOrderId = editViewModel.WorkOrderId,
                    Description = editViewModel.Description,
                    Note = editViewModel.Note,
                    WoTypeId = editViewModel.WoTypeId,
                    ProcurementType = editViewModel.ProcurementType,
                    WoNumLetter = editViewModel.WoNumLetter,
                    DateLetter = editViewModel.DateLetter,
                    From = editViewModel.From,
                    To = editViewModel.To,
                    WorkOrderLetter = editViewModel.WorkOrderLetter,
                    WBS = editViewModel.WBS,
                    GlAccount = editViewModel.GlAccount,
                    DateRequired = editViewModel.DateRequired,
                    Requester = editViewModel.Requester,
                    Approved = editViewModel.Approved,
                    XS1 = editViewModel.XS1,
                    XS2 = editViewModel.XS2,
                    XS3 = editViewModel.XS3,
                    XS4 = editViewModel.XS4,
                    StatusId = editViewModel.StatusId,
                };

                await _woService.EditWorkOrderAsync(
                    workOrder,
                    id,
                    editViewModel.Details ?? new List<WoDetail>(),
                    editViewModel.Offers ?? new List<WoOffer>()
                );
                TempData["SuccessMessage"] = "Work Order berhasil diupdate!";

                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "WorkOrder not found for edit, ID: {id}", id);
                TempData["ErrorMessage"] = ex.Message;
                ModelState.AddModelError("", ex.Message);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating WorkOrder, ID: {id}", id);
                ModelState.AddModelError(
                    "",
                    $"Terjadi kesalahan saat mengupdate data: {ex.Message}"
                );
                await RepopulateEditViewModel(editViewModel);
                return View(editViewModel);
            }
        }

        // GET: WorkOrders/Delete/5
        [Authorize(Policy = Permissions.WO.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(id);

                if (workOrder == null)
                    return NotFound();

                return View(workOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Work Order for delete, ID: {id}", id);
                TempData["ErrorMessage"] = $"Gagal memuat data untuk delete: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.WO.Delete)]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(id);
                if (workOrder == null)
                    return NotFound();

                await _woService.DeleteWorkOrderAsync(workOrder);
                TempData["SuccessMessage"] = "Work Order berhasil dihapus!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Gagal menghapus Work Order: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        //GET: WorkOrders/5/CreatePnl
        [HttpGet("WorkOrders/{workOrderId}/CreateProfitLoss")]
        public async Task<IActionResult> CreateProfitLoss(string workOrderId)
        {
            if (string.IsNullOrWhiteSpace(workOrderId))
            {
                TempData["ErrorMessage"] = "Work Order ID tidak valid";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(workOrderId);
                if (string.IsNullOrWhiteSpace(workOrderId) || workOrder == null)
                {
                    TempData["ErrorMessage"] = "Work Order tidak ditemukan";
                    return RedirectToAction("Index", "WorkOrder");
                }

                var vendors = await _vendorService.GetAllVendorsAsync();
                var viewModel = new ProfitLossInputViewModel
                {
                    WorkOrderId = workOrderId,
                    VendorChoices = vendors
                        .Select(vendor => new VendorChoiceViewModel
                        {
                            Id = vendor.VendorId,
                            Name = vendor.VendorName,
                        })
                        .ToList(),
                    WoItems = (workOrder.WoOffers ?? [])
                        .Select(o => new WoOfferLiteVm
                        {
                            WoOfferId = o.WoOfferId,
                            ItemPenawaran = o.ItemPenawaran,
                        })
                        .ToList(),
                };

                viewModel.Items = viewModel
                    .WoItems.Select(o => new ItemTariffInputVm
                    {
                        WoOfferId = o.WoOfferId,
                        TarifAwal = 0,
                        TarifAdd = 0,
                        KmPer25 = 0,
                        OperatorCost = 0m,
                    })
                    .ToList();

                ViewBag.WoNum = workOrder.WoNum;
                ViewBag.IssueDate = workOrder.CreatedAt.ToString("d MMMM yyyy");

                if (viewModel.WoItems.Count == 0)
                    TempData["WarningMessage"] =
                        "Belum ada Item Penawaran (WoOffer). Tambahkan di halaman Edit WO agar bisa input harga per item.";

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating ProfitLoss form for WorkOrder: {WorkOrderId}",
                    workOrderId
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
                ModelState.Remove($"Items[{key}].WoOfferId");
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
                    await _woService.GetWorkOrderByIdAsync(dto.WorkOrderId)
                    ?? throw new KeyNotFoundException("Work Order tidak ditemukan");

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

                var generateReq = new GeneratedWoDocumentRequest
                {
                    WorkOrderId = dto.WorkOrderId,
                    DocumentTypeId = pnlDocTypeId,
                    FileName = $"Profit_Loss_{wo.WoNum}.pdf",
                    ContentType = "application/pdf",
                    Bytes = pdfBytes,
                    Description = "Profit & Loss auto-generated",
                    GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    CreatedAt = DateTime.Now,
                };

                var saveResult = await _woDocService.SaveGeneratedAsync(generateReq);

                TempData["SuccessMessage"] =
                    "Profit & Loss berhasil dibuat & generate dokumen telah berhasil";

                return RedirectToAction(nameof(Details), new { id = dto.WorkOrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating ProfitLoss for WorkOrder: {WorkOrderId}",
                    viewModel.WorkOrderId
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

        // GET: /WorkOrders/EditPnL/5
        [HttpGet]
        public async Task<IActionResult> EditProfitLoss(string id)
        {
            _logger.LogInformation("🔵 EditPnL (GET) called with ID: {Id}", id);

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
                                "EditPnL Item[{Index}] WoOfferId={WoOfferId} TarifAwal={TarifAwal} TarifAdd={TarifAdd} KmPer25={KmPer25}",
                                index++,
                                item.WoOfferId,
                                item.TarifAwal,
                                item.TarifAdd,
                                item.KmPer25
                            );
                        }
                    }
                }
                var vendors = await _vendorService.GetAllVendorsAsync();
                var wo =
                    await _woService.GetWorkOrderByIdAsync(dto.WorkOrderId)
                    ?? throw new KeyNotFoundException("Work Order tidak ditemukan");

                var vm = new ProfitLossEditViewModel
                {
                    ProfitLossId = dto.ProfitLossId,
                    WorkOrderId = dto.WorkOrderId,
                    // agregat lama (TarifAwal/TarifAdd/KmPer25) TIDAK dipakai lagi
                    Items = dtoItems
                        .Select(x => new ItemTariffInputVm
                        {
                            WoOfferId = x.WoOfferId,
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
                                    WoOfferId = it.WoOfferId,
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
                    WoItems = (wo.WoOffers ?? [])
                        .Select(x => new WoOfferLiteVm
                        {
                            WoOfferId = x.WoOfferId,
                            ItemPenawaran = x.ItemPenawaran,
                        })
                        .ToList(),
                };

                ViewBag.WoNum = wo.WoNum;
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
                _logger.LogInformation("✅ EditPnL view akan ditampilkan");
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error di EditPnL");
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
                var wo = await _woService.GetWorkOrderByIdAsync(viewModel.WorkOrderId);
                viewModel.VendorChoices = vendors
                    .Select(v => new VendorChoiceViewModel { Id = v.VendorId, Name = v.VendorName })
                    .ToList();
                viewModel.WoItems = (wo?.WoOffers ?? [])
                    .Select(x => new WoOfferLiteVm
                    {
                        WoOfferId = x.WoOfferId,
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
                WorkOrderId = viewModel.WorkOrderId,
                Items = (viewModel.Items ?? [])
                    .Select(x => new ProfitLossItemInputDto
                    {
                        WoOfferId = x.WoOfferId,
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

        // WorkOrdersController.cs
        private static ProfitLossSummaryViewModel MapToViewModel(ProfitLossSummaryDto dto)
        {
            return new ProfitLossSummaryViewModel
            {
                ProfitLossId = dto.ProfitLossId,
                WorkOrderId = dto.WorkOrderId,

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
                    WoOfferId = x.WoOfferId,
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
                WorkOrderId = vm.WorkOrderId,
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
                    if (string.IsNullOrWhiteSpace(item.WoOfferId))
                        continue;

                    var dto = new VendorOfferPerItemDto
                    {
                        VendorId = vendorId,
                        WoOfferId = item.WoOfferId!,
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

            var wo = await _woService.GetWorkOrderByIdAsync(viewModel.WorkOrderId);
            viewModel.WoItems = (wo?.WoOffers ?? [])
                .Select(o => new WoOfferLiteVm
                {
                    WoOfferId = o.WoOfferId,
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
                ModelState.Remove($"Details[{key}].WorkOrderId");
                ModelState.Remove($"Details[{key}].WorkOrder");
                ModelState.Remove($"Details[{key}].WoDetailId");
            }
        }

        private void RemoveOffersValidation()
        {
            var offerKeys = Request.Form["Offers.Index"].ToArray();
            foreach (var key in offerKeys)
            {
                ModelState.Remove($"Offers[{key}].WorkOrderId");
                ModelState.Remove($"Offers[{key}].WorkOrder");
                ModelState.Remove($"Offers[{key}].WoOfferId");
            }
        }

        private async Task RepopulateCreateViewModel(WorkOrderCreateViewModel createViewModel)
        {
            ViewData["EnumProcurementTypes"] = Enum.GetValues<ProcurementType>();
            var woTypeName = (
                await _woService.GetWoTypeByIdAsync(
                    createViewModel.WorkOrder.WoTypeId ?? string.Empty
                )
            )?.TypeName;

            ViewBag.CreatePartial = ResolveCreatePartialByName(woTypeName);
            ViewBag.SelectedWoTypeName = woTypeName ?? "Other";
        }

        private async Task RepopulateEditViewModel(WorkOrderEditViewModel viewModel)
        {
            var (woTypes, statuses) = await _woService.GetRelatedEntitiesForWorkOrderAsync();
            viewModel.WoTypes = woTypes;
            viewModel.Statuses = statuses;

            ViewData["EnumProcurementTypes"] = Enum.GetValues<ProcurementType>();

            var woTypeName = woTypes
                .FirstOrDefault(w => w.WoTypeId == viewModel.WoTypeId)
                ?.TypeName;
            ViewBag.CreatePartial = ResolveCreatePartialByName(woTypeName);
            ViewBag.SelectedWoTypeName = woTypeName ?? "Other";
        }

        #endregion
    }
}
