using System.ComponentModel.Design;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        IPdfGenerator pdfGenerator,
        IDocumentTypeService docTypeService,
        IWoDocumentService woDocService,
        ILogger<WorkOrdersController> logger
    ) : Controller
    {
        private readonly IWorkOrderService _woService = woService;
        private readonly IVendorService _vendorService = vendorService;
        private readonly IProfitLossService _pnlService = pnlService;
        private readonly IVendorOfferService _voService = voService;
        private readonly IPdfGenerator _pdfGenerator = pdfGenerator;
        private readonly IDocumentTypeService _docTypeService = docTypeService;
        private readonly IWoDocumentService _woDocService = woDocService;
        private readonly ILogger<WorkOrdersController> _logger = logger;

        // GET: WorkOrders
        // Work Order Index Page
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
        // Work Order Detail Page
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
        // Work Order select type WO Page
        public async Task<IActionResult> SelectType()
        {
            var related = await _woService.GetRelatedEntitiesForWorkOrderAsync();
            return View(related.WoTypes);
        }

        // POST: WorkOrders/SelectType
        // Work Order selected type
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SelectType(string woTypeId)
        {
            return RedirectToAction(nameof(CreateByType), new { woTypeId });
        }

        // GET: WorkOrders/CreateByType?woTypeId/1
        // Form Work Order Type Selected Page
        [HttpGet]
        public async Task<IActionResult> CreateByType(string woTypeId)
        {
            var woType = await _woService.GetWoTypeByIdAsync(woTypeId);
            if (woType is null)
            {
                return NotFound();
            }

            ViewData["EnumProcurementTypes"] = Enum.GetValues<ProcurementType>();
            ViewBag.SelectedWoTypeName = woType.TypeName;

            var createWoVM = new WorkOrderCreateViewModel
            {
                WorkOrder = new WorkOrder { WoTypeId = woTypeId },
            };
            ViewBag.CreatePartial = ResolveCreatePartialByName(woType.TypeName);
            return View("CreateByType", createWoVM);
        }

        // POST: WorkOrders/CreateByType?woTypeId/1
        // Post form work order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromForm] WorkOrderCreateViewModel woViewModel,
            string submitAction
        )
        {
            //ModelState.Remove(nameof(User));
            ModelState.Remove("WorkOrder.WoNum");
            //ModelState.Remove("WorkOrder.WorkOrderId");

            var detailKeys = Request.Form["Details.Index"].ToArray();
            foreach (var key in detailKeys)
            {
                ModelState.Remove($"Details[{key}].WorkOrderId");
                ModelState.Remove($"Details[{key}].WorkOrder");
                ModelState.Remove($"Details[{key}].WoDetailId");
            }

            woViewModel ??= new WorkOrderCreateViewModel();
            woViewModel.Details ??= new List<WoDetail>();
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
                ViewData["EnumProcurementTypes"] = Enum.GetValues<ProcurementType>();

                var woTypeName = (
                    await _woService.GetWoTypeByIdAsync(
                        woViewModel.WorkOrder.WoTypeId ?? string.Empty
                    )
                )?.TypeName;
                ViewBag.CreatePartial = ResolveCreatePartialByName(woTypeName);
                ViewBag.SelectedWoTypeName = woTypeName ?? "Other";

                return View("CreateByType", woViewModel);
            }

            try
            {
                await _woService.AddWorkOrderWithDetailsAsync(
                    woViewModel.WorkOrder,
                    woViewModel.Details!
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

            var fallbackName = (
                await _woService.GetWoTypeByIdAsync(woViewModel.WorkOrder.WoTypeId ?? string.Empty)
            )?.TypeName;
            ViewBag.CreatePartial = ResolveCreatePartialByName(fallbackName);
            ViewBag.SelectedWoTypeName = fallbackName ?? "Other";
            return View("CreateByType", woViewModel);
        }

        // GET: WorkOrders/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var workOrder = await _woService.GetWorkOrderByIdAsync(id);

            if (workOrder == null)
                return NotFound();

            try
            {
                return View(workOrder);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat data untuk diedit: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WorkOrder workOrder, string id)
        {
            ModelState.Remove(nameof(WorkOrder.User));

            if (id != workOrder.WorkOrderId)
            {
                return NotFound();
            }

            try
            {
                _logger.LogInformation("kesalahan");
                if (ModelState.IsValid)
                {
                    await _woService.EditWorkOrderAsync(workOrder, id);
                    TempData["SuccessMessage"] = "Work Order berhasil diupdate!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (KeyNotFoundException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return NotFound();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Terjadi kesalahan saat mengupdate data: " + ex.Message
                );
            }

            return View(workOrder);
        }

        // GET: WorkOrders/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(id);

                if (workOrder == null)
                {
                    return NotFound();
                }

                return View(workOrder);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat data untuk delete: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, IFormCollection collection)
        {
            ArgumentNullException.ThrowIfNull(collection);
            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(id);

                if (workOrder == null)
                {
                    return NotFound();
                }

                await _woService.DeleteWorkOrderAsync(workOrder);
                TempData["SuccessMessage"] = "Work Order berhasil dihapus!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menghapus Work Order: " + ex.Message;
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
                };

                ViewBag.WoNum = workOrder.WoNum;
                ViewBag.IssueDate = workOrder.CreatedAt.ToString("d MMMM yyyy");

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

                var wo = await _woService.GetWorkOrderByIdAsync(dto.WorkOrderId)!;
                var offers = await _voService.GetByWorkOrderAsync(pnl.WorkOrderId);
                Vendor? bestVendor = null;

                if (!string.IsNullOrWhiteSpace(pnl.SelectedVendorId))
                {
                    bestVendor = (await _vendorService.GetAllVendorsAsync()).FirstOrDefault(
                        vendor => vendor.VendorId == pnl.SelectedVendorId
                    );
                }

                var pdfBytes = await _pdfGenerator.GenerateProfitLossPdfAsync(
                    pnl,
                    wo!,
                    bestVendor,
                    offers
                );

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
                    FileName = $"Profit_Loss_{wo!.WoNum}.pdf",
                    ContentType = "application/pdf",
                    Bytes = pdfBytes,
                    Description = "Profit & Loss auto-generated",
                    GeneratedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    CreatedAt = DateTime.Now,
                };

                var saveResult = await _woDocService.SaveGeneratedAsync(generateReq);

                TempData["Success"] =
                    "Profit & Loss berhasil dibuat & generate dokumen telah berhasil";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in CreateProfitLoss");
                ModelState.AddModelError("", ex.Message);
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
            return View(viewModel);
        }

        // GET: /WorkOrders/EditPnL/5
        [HttpGet]
        public async Task<IActionResult> EditProfitLoss(string id)
        {
            _logger.LogInformation($"🔵 EditPnL (GET) called with ID: {id}");

            try
            {
                var dto = await _pnlService.GetEditDataAsync(id);
                var vendors = await _vendorService.GetAllVendorsAsync();

                var viewModel = new ProfitLossEditViewModel
                {
                    ProfitLossId = dto.ProfitLossId,
                    WorkOrderId = dto.WorkOrderId,
                    TarifAwal = dto.TarifAwal,
                    TarifAdd = dto.TarifAdd,
                    KmPer25 = dto.KmPer25,
                    SelectedVendorIds = dto.SelectedVendorIds.ToList(),
                    Vendors = dto
                        .Vendors.Select(vendor => new VendorOfferInputViewModel
                        {
                            VendorId = vendor.VendorId,
                            Prices = vendor.Prices,
                        })
                        .ToList(),
                    VendorChoices = vendors
                        .Select(vendor => new VendorChoiceViewModel
                        {
                            Id = vendor.VendorId,
                            Name = vendor.VendorName,
                        })
                        .ToList(),
                };

                var wo = await _woService.GetWorkOrderByIdAsync(dto.WorkOrderId);
                ViewBag.WoNum = wo!.WoNum;
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
                _logger.LogInformation($"✅ EditPnL view akan ditampilkan");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error di EditPnL: {ex.Message}\n{ex.StackTrace}");
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
                viewModel.VendorChoices = vendors
                    .Select(vendor => new VendorChoiceViewModel
                    {
                        Id = vendor.VendorId,
                        Name = vendor.VendorName,
                    })
                    .ToList();

                return View(viewModel);
            }

            var update = new ProfitLossUpdateDto
            {
                ProfitLossId = viewModel.ProfitLossId,
                WorkOrderId = viewModel.WorkOrderId,
                TarifAwal = viewModel.TarifAwal,
                TarifAdd = viewModel.TarifAdd,
                KmPer25 = viewModel.KmPer25,
                SelectedVendorIds = viewModel.SelectedVendorIds?.Distinct().ToList() ?? [],
                Vendors = (viewModel.Vendors ?? [])
                    .Where(vendor =>
                        vendor.Prices != null && vendor.Prices.Any(price => price > 0m)
                    )
                    .Select(item => new VendorOffersDto
                    {
                        VendorId = item.VendorId,
                        Prices = item.Prices.Where(price => price > 0m).ToList(),
                    })
                    .ToList(),
            };

            await _pnlService.EditProfitLossAsync(update);
            TempData["Success"] = "Profit & Loss berhasil diupdate";

            return RedirectToAction(nameof(Index));
        }

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

        private ProfitLossSummaryViewModel MapToViewModel(ProfitLossSummaryDto dto)
        {
            return new ProfitLossSummaryViewModel
            {
                ProfitLossId = dto.ProfitLossId,
                WorkOrderId = dto.WorkOrderId,
                TarifAwal = dto.TarifAwal,
                TarifAdd = dto.TarifAdd,
                KmPer25 = dto.KmPer25,
                OperatorCost = dto.OperatorCost,
                Revenue = dto.Revenue,
                SelectedVendorId = dto.SelectedVendorId,
                SelectedVendorName = dto.SelectedVendorName,
                SelectedFinalOffer = dto.SelectedFinalOffer,
                Profit = dto.Profit,
                ProfitPercent = dto.ProfitPercent,
                Rows = dto
                    .VendorComparisons.Select(vendor =>
                        (
                            vendor.VendorName,
                            vendor.FinalOffer,
                            vendor.Profit,
                            vendor.ProfitPercent,
                            vendor.IsSelected
                        )
                    )
                    .ToList(),
            };
        }

        private ProfitLossInputDto MapToDto(
            ProfitLossInputViewModel viewModel,
            List<string> selectedVendors
        )
        {
            var withOffers = viewModel
                .Vendors.Where(v => v.Prices != null && v.Prices.Any(p => p > 0m))
                .Where(v => selectedVendors.Contains(v.VendorId))
                .Select(v => new VendorOffersDto
                {
                    VendorId = v.VendorId,
                    Prices = v.Prices.Where(p => p > 0m).ToList(),
                })
                .ToList();

            return new ProfitLossInputDto
            {
                WorkOrderId = viewModel.WorkOrderId,
                TarifAwal = viewModel.TarifAwal,
                TarifAdd = viewModel.TarifAdd,
                KmPer25 = viewModel.KmPer25,
                SelectedVendorIds = selectedVendors,
                Vendors = withOffers,
            };
        }

        private async Task RepopulateVendorChoices(ProfitLossInputViewModel viewModel)
        {
            var vendors = await _vendorService.GetAllVendorsAsync();
            viewModel.VendorChoices = vendors
                .Select(v => new VendorChoiceViewModel { Id = v.VendorId, Name = v.VendorName })
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

        #endregion
    }
}
