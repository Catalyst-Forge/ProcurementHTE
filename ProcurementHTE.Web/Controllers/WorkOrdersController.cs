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
        ILogger<WorkOrdersController> logger
    ) : Controller
    {
        private readonly IWorkOrderService _woService = woService;
        private readonly IVendorService _vendorService = vendorService;
        private readonly IProfitLossService _pnlService = pnlService;
        private readonly ILogger<WorkOrdersController> _logger = logger;

        // GET: WorkOrders
        public async Task<IActionResult> Index()
        {
            var workOrders = await _woService.GetAllWorkOrderWithDetailsAsync();
            ViewBag.TotalWo = workOrders.Count();
            ViewBag.ActivePage = "Index Work Orders";

            return View(workOrders);
        }

        // GET: WorkOrders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (!string.IsNullOrEmpty(id))
                try
                {
                    var workOrder = await _woService.GetWorkOrderByIdAsync(id);
                    if (workOrder != null)
                    {
                        var pnl = await _pnlService.GetProfitLossWithWorkOrderAsync(
                            workOrder.WorkOrderId
                        );
                        if (pnl != null)
                        {
                            decimal displayAdjustmentRate =
                                (pnl.AdjustmentRate ?? 0m) <= 1m
                                    ? (pnl.AdjustmentRate ?? 0m) * 100m
                                    : (pnl.AdjustmentRate ?? 0m);

                            var pnlViewModel = new EditProfitLossViewModel
                            {
                                ProfitLossId = pnl.ProfitLossId,
                                WorkOrderId = pnl.WorkOrderId,
                                WoNum = pnl.WorkOrder?.WoNum ?? "-",
                                VendorName = pnl.SelectedVendorOffer?.Vendor?.VendorName ?? "-",
                                Revenue = pnl.Revenue,
                                CostOperator = pnl.CostOperator,
                                Profit = pnl.Profit,
                                ProfitPercentage = pnl.ProfitPercentage,
                                Penawaran1Price = pnl.HargaPenawaran1,
                                Penawaran2Price = pnl.HargaPenawaran2,
                                Penawaran3Price = pnl.HargaPenawaran3,
                                BestOfferPrice = pnl.BestOfferPrice,
                                AdjustmentRate = displayAdjustmentRate,
                                AdjustedProfit = pnl.AdjustedProfit,
                            };

                            ViewBag.PnlViewModel = pnlViewModel;
                        }

                        return View(workOrder);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Gagal memuat detail Work Order: " + ex.Message;
                    return RedirectToAction(nameof(Index));
                }

            return NotFound();
        }

        // GET: WorkOrders/Create
        public async Task<IActionResult> SelectType()
        {
            var related = await _woService.GetRelatedEntitiesForWorkOrderAsync();
            return View(related.WoTypes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SelectType(int woTypeId)
        {
            return RedirectToAction(nameof(CreateByType), new { woTypeId });
        }

        [HttpGet]
        public async Task<IActionResult> CreateByType(string woTypeId)
        {
            var woType = await _woService.GetWoTypeByIdAsync(woTypeId);
            if (woType is null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(selectedWoTypeId: woTypeId);

            ViewData["EnumProcurementTypes"] = Enum.GetValues<ProcurementType>();
            ViewBag.SelectedWoTypeName = woType.TypeName;

            var createWoVM = new WorkOrderCreateViewModel
            {
                WorkOrder = new WorkOrder { WoTypeId = woTypeId },
            };
            ViewBag.CreatePartial = ResolveCreatePartialByName(woType.TypeName);
            return View("CreateByType", createWoVM);
        }

        // POST: WorkOrders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromForm] WorkOrderCreateViewModel woViewModel,
            string submitAction
        )
        {
            ModelState.Remove(nameof(User));
            woViewModel ??= new WorkOrderCreateViewModel();
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
                await PopulateDropdownsAsync(
                    woViewModel.WorkOrder.StatusId,
                    woViewModel.WorkOrder.WoTypeId
                );
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
                    woViewModel.Details
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
            await PopulateDropdownsAsync(
                woViewModel.WorkOrder.StatusId,
                woViewModel.WorkOrder.WoTypeId
            );

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
                await PopulateDropdownsAsync(workOrder.StatusId, workOrder.WoTypeId);
                return View(workOrder);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat data untuk diedit: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkOrders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

            await PopulateDropdownsAsync(workOrder.StatusId, workOrder.WoTypeId);
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

        // GET: WorkOrders/5/CreatePnl
        [HttpGet("WorkOrders/{workOrderId}/CreatePnL")]
        public async Task<IActionResult> CreatePnL(string workOrderId)
        {
            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(workOrderId);
                if (workOrder == null)
                {
                    TempData["ErrorMessage"] = "Work Order tidak ditemukan";
                    return RedirectToAction("Index", "WorkOrder");
                }

                var vendors = await _vendorService.GetAllVendorsAsync();

                var viewModel = new CreateProfitLossViewModel
                {
                    WorkOrderId = workOrderId,
                    WoNum = workOrder.WoNum!,
                    IssuedDate = workOrder.CreatedAt,
                    Vendors = vendors
                        .Select(v => new SelectListItem
                        {
                            Value = v.VendorId.ToString(),
                            Text = v.VendorName,
                        })
                        .ToList(),
                    AdjustmentRate = 15,
                };

                for (int i = 0; i < 3; i++)
                {
                    viewModel.VendorOffers.Add(
                        new VendorOfferRowDto { RowIndex = i + 1, OfferNumber = i + 1 }
                    );
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index", "WorkOrders");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePnLPost(
            [FromForm] CreateProfitLossViewModel viewModel
        )
        {
            _logger.LogInformation($"🔵 CreatePnLPost (POST) called");
            _logger.LogInformation($"ModelState.IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState tidak valid");
                var errors = ModelState.Values.SelectMany(x => x.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning($"  - {error.ErrorMessage}");
                }

                var vendors = await _vendorService.GetAllVendorsAsync();
                viewModel.Vendors = vendors
                    .Select(v => new SelectListItem
                    {
                        Value = v.VendorId.ToString(),
                        Text = v.VendorName,
                    })
                    .ToList();
                return View("CreatePnL", viewModel);
            }

            try
            {
                string? bestVendor = viewModel.SelectedBestVendorId;

                // Validasi minimal ada 1 penawaran
                var validOffers = viewModel
                    .VendorOffers.Where(x =>
                        !string.IsNullOrWhiteSpace(x.VendorId) && x.OfferPrice > 0
                    )
                    .ToList();

                _logger.LogInformation($"Valid offers: {validOffers.Count}");

                if (!validOffers.Any())
                {
                    ModelState.AddModelError("", "Minimal harus ada 1 penawaran vendor");
                    var vendors = await _vendorService.GetAllVendorsAsync();
                    viewModel.Vendors = vendors
                        .Select(v => new SelectListItem
                        {
                            Value = v.VendorId.ToString(),
                            Text = v.VendorName,
                        })
                        .ToList();
                    return View("CreatePnL", viewModel);
                }

                if (string.IsNullOrWhiteSpace(bestVendor))
                {
                    var lowestOffer = validOffers.MinBy(offer => offer.OfferPrice);
                    bestVendor = lowestOffer.VendorId;
                    _logger.LogInformation(
                        $"Sistem otomatis pilih best vendor: {lowestOffer.VendorId} ({lowestOffer.OfferPrice})"
                    );
                }
                else
                {
                    _logger.LogInformation($"🟠 User pilih manual vendor terbaik ID: {bestVendor}");
                }

                // Validasi selected vendor ada di list
                var selectedVendorOffer = validOffers.FirstOrDefault(x =>
                    x.VendorId == viewModel.SelectedBestVendorId
                );
                if (selectedVendorOffer == null)
                {
                    _logger.LogWarning(
                        $"Selected vendor {viewModel.SelectedBestVendorId} tidak ada di list penawaran"
                    );
                    ModelState.AddModelError(
                        "",
                        "Vendor yang dipilih harus ada di daftar penawaran"
                    );

                    var vendors = await _vendorService.GetAllVendorsAsync();
                    viewModel.Vendors = vendors
                        .Select(v => new SelectListItem
                        {
                            Value = v.VendorId.ToString(),
                            Text = v.VendorName,
                        })
                        .ToList();
                    return View("CreatePnL", viewModel);
                }

                _logger.LogInformation(
                    $"Membuat VendorOfferInputDto untuk {validOffers.Count} penawaran..."
                );

                // Prepare DTOs
                var vendorOfferDtos = validOffers.Select(x => new VendorOfferInputDto
                {
                    VendorId = x.VendorId!,
                    ItemName = x.ItemName,
                    Trip = x.Trip,
                    Unit = x.Unit,
                    OfferNumber = x.OfferNumber,
                    OfferPrice = x.OfferPrice,
                });

                var plInputDto = new CreateProfitLossInputDto
                {
                    WorkOrderId = viewModel.WorkOrderId,
                    Revenue = viewModel.Revenue,
                    CostOperator = viewModel.CostOperator,
                    AdjustmentRate = viewModel.AdjustmentRate,
                    SelectedVendorId = viewModel.SelectedBestVendorId!,
                };

                _logger.LogInformation($"Calling CreateProfitLossWithOffersAsync...");

                // Create P&L
                var profitLoss = await _pnlService.CreateProfitLossWithOffersAsync(
                    vendorOfferDtos,
                    plInputDto
                );

                _logger.LogInformation(
                    $"✅ ProfitLoss berhasil dibuat dengan ID: {profitLoss.ProfitLossId}"
                );

                TempData["SuccessMessage"] = "Profit & Loss berhasil dibuat!";
                return RedirectToAction("EditPnL", new { id = profitLoss.ProfitLossId });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error di CreatePnLPost: {ex.Message}\n{ex.StackTrace}");
                ModelState.AddModelError("", $"Error: {ex.Message}");
                var vendors = await _vendorService.GetAllVendorsAsync();
                viewModel.Vendors = vendors
                    .Select(v => new SelectListItem
                    {
                        Value = v.VendorId.ToString(),
                        Text = v.VendorName,
                    })
                    .ToList();
                return View("CreatePnL", viewModel);
            }

            return View("CreatePnL", viewModel);
        }

        /// <summary>
        /// GET: /WorkOrders/EditPnL/5
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditPnL(string id)
        {
            _logger.LogInformation($"🔵 EditPnL (GET) called with ID: {id}");

            try
            {
                var profitLoss = await _pnlService.GetProfitLossByIdAsync(id);
                if (profitLoss == null)
                {
                    _logger.LogWarning($"ProfitLoss dengan ID {id} tidak ditemukan");
                    TempData["ErrorMessage"] = "Data tidak ditemukan";
                    return RedirectToAction("Index");
                }

                var viewModel = new EditProfitLossViewModel
                {
                    ProfitLossId = profitLoss.ProfitLossId,
                    WoNum = profitLoss.WorkOrder?.WoNum ?? "-",
                    VendorName = profitLoss.SelectedVendorOffer?.Vendor?.VendorName ?? "-",
                    Revenue = profitLoss.Revenue,
                    CostOperator = profitLoss.CostOperator,
                    Profit = profitLoss.Profit,
                    ProfitPercentage = profitLoss.ProfitPercentage,
                    Penawaran1Price = profitLoss.HargaPenawaran1,
                    Penawaran2Price = profitLoss.HargaPenawaran2,
                    Penawaran3Price = profitLoss.HargaPenawaran3,
                    BestOfferPrice = profitLoss.BestOfferPrice,
                    AdjustmentRate = profitLoss.AdjustmentRate * 100,
                    AdjustedProfit = profitLoss.AdjustedProfit,
                };

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

        //[HttpGet] // -> /WorkOrders/PnLDetails/{id}
        [HttpGet("WorkOrders/PnLDetails/{id}")]
        public async Task<IActionResult> PnLDetails(string id)
        {
            _logger.LogInformation("🔵 PnLDetails (GET) id={Id}", id);

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "ID P&L tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var pl = await _pnlService.GetProfitLossByIdAsync(id);
                if (pl is null)
                {
                    TempData["ErrorMessage"] = "Data Profit & Loss tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                // Normalisasi tampilan AdjustmentRate:
                // - jika disimpan 0.15 => tampilkan 15
                // - jika disimpan 15   => tampilkan 15
                decimal displayAdjRate =
                    (pl.AdjustmentRate ?? 0m) <= 1m
                        ? (pl.AdjustmentRate ?? 0m) * 100m
                        : (pl.AdjustmentRate ?? 0m);

                var vm = new EditProfitLossViewModel
                {
                    ProfitLossId = pl.ProfitLossId,
                    WorkOrderId = pl.WorkOrderId,
                    WoNum = pl.WorkOrder?.WoNum ?? "-",
                    VendorName = pl.SelectedVendorOffer?.Vendor?.VendorName ?? "-",
                    Revenue = pl.Revenue,
                    CostOperator = pl.CostOperator,
                    Profit = pl.Profit,
                    ProfitPercentage = pl.ProfitPercentage,
                    Penawaran1Price = pl.HargaPenawaran1,
                    Penawaran2Price = pl.HargaPenawaran2,
                    Penawaran3Price = pl.HargaPenawaran3,
                    BestOfferPrice = pl.BestOfferPrice,
                    AdjustmentRate = displayAdjRate, // persen untuk ditampilkan
                    AdjustedProfit = pl.AdjustedProfit,
                };

                return View("PnLDetails", vm); // Views/WorkOrders/PnLDetails.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Gagal memuat PnLDetails id={Id}", id);
                TempData["ErrorMessage"] = "Terjadi kesalahan saat memuat Profit & Loss.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PopulateDropdownsAsync(
            int? selectedStatusId = null,
            string? selectedWoTypeId = null
        )
        {
            var relatedEntities = await _woService.GetRelatedEntitiesForWorkOrderAsync();
            ViewData["StatusId"] = new SelectList(
                relatedEntities.Statuses,
                "StatusId",
                "StatusName",
                selectedStatusId
            );
            ViewData["WoTypeId"] = new SelectList(
                relatedEntities.WoTypes,
                "WoTypeId",
                "TypeName",
                selectedWoTypeId
            );
        }

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
    }
}
