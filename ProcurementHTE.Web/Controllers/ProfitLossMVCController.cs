using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Repositories;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers {
    [Controller]
    [Route("[controller]")]
    public class ProfitLossMVCController : Controller {

        private readonly IProfitLossService _profitLossService;
        private readonly IWorkOrderService _workOrderService;
        private readonly IVendorOfferService _vendorOfferService;
        private readonly IVendorService _vendorService;

        public ProfitLossMVCController(IProfitLossService profitLossService,
        IWorkOrderService workOrderService, IVendorService venderService, IVendorOfferService vendorOfferService) {
            _profitLossService = profitLossService;
            _workOrderService = workOrderService;
            _vendorOfferService = vendorOfferService;
            _vendorService = venderService;
        }

        [HttpGet("index")]
        public async Task<IActionResult> Index() {
            try {
                var profitLosses = await _profitLossService.GetAllProfitLossAsync();
                var viewModels = profitLosses.Select(pl => new ProfitLossViewModel {
                    ProfitLossId = pl.ProfitLossId,
                    WoNum = pl.WorkOrder?.WoNum!,
                    VendorName = pl.SelectedVendorOffer?.Vendor?.VendorName!,
                    Revenue = pl.Revenue,
                    CostOperator = pl.CostOperator,
                    Profit = pl.Profit,
                    ProfitPercentage = pl.ProfitPercentage,
                    BestOfferPrice = pl.BestOfferPrice,
                    AdjustmentRate = pl.AdjustmentRate * 100,
                    AdjustedProfit = pl.AdjustedProfit,
                    CreatedAt = pl.CreatedAt
                }).ToList();

                return View(viewModels);
            } catch (Exception ex) {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(new List<ProfitLossViewModel>());
            }
        }

        [HttpGet("create/{workOrderId}")]
        public async Task<IActionResult> Create(string workOrderId) {
            try {
                var workOrder = await _workOrderService.GetWorkOrderByIdAsync(workOrderId);
                if (workOrder == null) {
                    TempData["ErrorMessage"] = "Work Order tidak ditemukan";
                    return RedirectToAction("Index", "WorkOrder");
                }

                var vendors = await _vendorService.GetAllVendorsAsync();

                var viewModel = new CreateProfitLossViewModel {
                    WorkOrderId = workOrderId,
                    WoNum = workOrder.WoNum!,
                    Vendors = vendors.Select(v => new SelectListItem {
                        Value = v.VendorId.ToString(),
                        Text = v.VendorName
                    }).ToList(),
                    AdjustmentRate = 15
                };

                for (int i = 0; i < 3; i++) {
                    viewModel.VendorOffers.Add(new VendorOfferRowDto {
                        RowIndex = i + 1,
                        OfferNumber = i + 1
                    });
                }

                return View(viewModel);
            } catch (Exception ex) {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index", "WorkOrder");
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePost([FromForm] CreateProfitLossViewModel viewModel) {
            try {
                if (!ModelState.IsValid) {
                    var vendors = await _vendorService.GetAllVendorsAsync();
                    viewModel.Vendors = vendors.Select(v => new SelectListItem {
                        Value = v.VendorId.ToString(),
                        Text = v.VendorName
                    }).ToList();
                    return View("CreateWithOffers", viewModel);
                }

                var validOffers = viewModel.VendorOffers
                .Where(x => x.VendorId != null && x.OfferPrice > 0)
                .ToList();

                if (!validOffers.Any()) {
                    ModelState.AddModelError("", "Minimal harus ada 1 penawaran vendor");
                    var vendors = await _vendorService.GetAllVendorsAsync();
                    viewModel.Vendors = vendors.Select(v => new SelectListItem {
                        Value = v.VendorId.ToString(),
                        Text = v.VendorName
                    }).ToList();
                    return View("CreateWithOffers", viewModel);
                }

                if (viewModel.SelectedBestVendorId == null) {
                    ModelState.AddModelError("", "Silakan pilih vendor terbaik");
                    var vendors = await _vendorService.GetAllVendorsAsync();
                    viewModel.Vendors = vendors.Select(v => new SelectListItem {
                        Value = v.VendorId.ToString(),
                        Text = v.VendorName
                    }).ToList();
                    return View("CreateWithOffers", viewModel);
                }

                var selectedVendorOffer = validOffers.FirstOrDefault(x => x.VendorId == viewModel.SelectedBestVendorId);
                if (selectedVendorOffer == null) {
                    ModelState.AddModelError("", "Vendor yang dipilih harus ada di daftar penawaran");
                    var vendors = await _vendorService.GetAllVendorsAsync();
                    viewModel.Vendors = vendors.Select(v => new SelectListItem {
                        Value = v.VendorId.ToString(),
                        Text = v.VendorName
                    }).ToList();
                    return View("CreateWithOffers", viewModel);
                }

                var vendorOfferDtos = validOffers.Select(x => new VendorOfferInputDto {
                    VendorId = x.VendorId,
                    OfferNumber = x.OfferNumber,
                    OfferPrice = x.OfferPrice
                }).ToList();

                var plInputDto = new CreateProfitLossInputDto {
                    WorkOrderId = viewModel.WorkOrderId,
                    SelectedVendorId = viewModel.SelectedBestVendorId,
                    CostOperator = viewModel.CostOperator,
                    AdjustmentRate = viewModel.AdjustmentRate
                };

                // Create PnL dengan VendorOffers
                var profitLoss = await _profitLossService.CreateProfitLossWithOffersAsync(
                    vendorOfferDtos,
                    plInputDto);

                TempData["SuccessMessage"] = "Profit & Loss berhasil dibuat!";
                return RedirectToAction("Edit", new { id = profitLoss.ProfitLossId });
            } catch (Exception ex) {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                var vendors = await _vendorService.GetAllVendorsAsync();
                viewModel.Vendors = vendors.Select(v => new SelectListItem {
                    Value = v.VendorId.ToString(),
                    Text = v.VendorName
                }).ToList();
                return View("CreateWithOffers", viewModel);
            }
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(string id) {
            try {
                var profitLoss = await _profitLossService.GetProfitLossWithWorkOrderAsync(id);
                if (profitLoss == null) {
                    TempData["ErrorMessage"] = "Data tidak ditemukan";
                    return RedirectToAction("Index");
                }

                var viewModel = new EditProfitLossViewModel {
                    ProfitLossId = profitLoss.ProfitLossId,
                    WoNum = profitLoss.WorkOrder.WoNum!,
                    VendorName = profitLoss.SelectedVendorOffer.Vendor.VendorName,
                    Revenue = profitLoss.Revenue,
                    CostOperator = profitLoss.CostOperator,
                    Profit = profitLoss.Profit,
                    ProfitPercentage = profitLoss.ProfitPercentage,
                    Penawaran1Price = profitLoss.HargaPenawaran1,
                    Penawaran2Price = profitLoss.HargaPenawaran2,
                    Penawaran3Price = profitLoss.HargaPenawaran3,
                    BestOfferPrice = profitLoss.BestOfferPrice,
                    AdjustmentRate = profitLoss.AdjustmentRate * 100,
                    AdjustedProfit = profitLoss.AdjustedProfit
                };

                return View(viewModel);
            } catch (Exception ex) {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> EditPost(string id, [FromForm] EditProfitLossViewModel viewModel) {
            try {
                var dto = new UpdateProfitLossDto {
                    CostOperator = viewModel.CostOperator,
                    AdjustmentRate = viewModel.AdjustmentRate
                };

                await _profitLossService.UpdateProfitLossAsync(id, dto);
                TempData["SuccessMessage"] = "Profit Loss berhasil diperbarui!";
                return RedirectToAction("Index");
            } catch (Exception ex) {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View("Edit", viewModel);
            }
        }

        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(string id) {
            try {
                var profitLoss = await _profitLossService.GetProfitLossWithWorkOrderAsync(id);
                if (profitLoss == null) {
                    TempData["ErrorMessage"] = "Data tidak ditemukan";
                    return RedirectToAction("Index");
                }

                return View(new DeleteProfitLossViewModel {
                    ProfitLossId = profitLoss.ProfitLossId,
                    WoNum = profitLoss.WorkOrder.WoNum!,
                    VendorName = profitLoss.SelectedVendorOffer.Vendor.VendorName,
                    Profit = profitLoss.Profit
                });

            } catch (Exception ex) {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        // POST: ProfitLossMVC/DeleteConfirmed/5
        [HttpPost("delete-confirmed/{id}")]
        public async Task<IActionResult> DeleteConfirmed(string id) {
            try {
                await _profitLossService.DeleteProfitLossAsync(id);
                TempData["SuccessMessage"] = "Profit Loss berhasil dihapus!";
                return RedirectToAction("Index");
            } catch (Exception ex) {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return RedirectToAction("Delete", new { id });
            }
        }
    }
}
