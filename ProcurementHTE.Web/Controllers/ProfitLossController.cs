//using AspNetCoreGeneratedDocument;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using ProcurementHTE.Core.Interfaces;
//using ProcurementHTE.Core.Models;
//using ProcurementHTE.Core.Models.DTOs;
//using ProcurementHTE.Infrastructure.Repositories;
//using ProcurementHTE.Web.Models.ViewModels;

//namespace ProcurementHTE.Web.Controllers
//{
//    public class ProfitLossController : Controller
//    {
//        private readonly IProfitLossService _profitLossService;
//        private readonly IVendorService _vendorService;
//        private readonly IVendorOfferService _voService;

//        public ProfitLossController(
//            IProfitLossService profitLossService,
//            IVendorService venderService,
//            IVendorOfferService voService
//        )
//        {
//            _profitLossService = profitLossService;
//            _vendorService = venderService;
//            _voService = voService;
//        }

//        [HttpGet]
//        public async Task<IActionResult> Create(string procurementId)
//        {
//            if (string.IsNullOrWhiteSpace(procurementId))
//                return BadRequest("Work Order wajib diisi");

//            var vendorList = await _vendorService.GetAllVendorsAsync();
//            var vm = new ProfitLossInputViewModel
//            {
//                ProcurementId = procurementId,
//                VendorChoices = vendorList
//                    .Select(vendor => new VendorChoiceViewModel
//                    {
//                        Id = vendor.VendorId,
//                        Name = vendor.VendorName,
//                    })
//                    .ToList(),
//            };

//            return View(vm);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(ProfitLossInputViewModel vm)
//        {
//            var vendorList = await _vendorService.GetAllVendorsAsync();
//            if (!ModelState.IsValid)
//            {
//                vm.VendorChoices = vendorList
//                    .Select(vendor => new VendorChoiceViewModel
//                    {
//                        Id = vendor.VendorId,
//                        Name = vendor.VendorName,
//                    })
//                    .ToList();

//                return View(vm);
//            }

//            var selectedVendors = vm.SelectedVendorIds?.Distinct().ToList() ?? [];
//            if (selectedVendors.Count == 0)
//            {
//                ModelState.AddModelError(nameof(vm.SelectedVendorIds), "Pilih minimal 1 vendor");
//                vm.VendorChoices = vendorList
//                    .Select(vendor => new VendorChoiceViewModel
//                    {
//                        Id = vendor.VendorId,
//                        Name = vendor.VendorName,
//                    })
//                    .ToList();

//                return View(vm);
//            }

//            var withOffers = vm
//                .Vendors.Where(item => item.Prices != null && item.Prices.Any(price => price > 0m))
//                .Where(item => selectedVendors.Contains(item.VendorId))
//                .Select(item => new VendorOffersDto
//                {
//                    VendorId = item.VendorId,
//                    Prices = item.Prices.Where(price => price > 0m).ToList(),
//                })
//                .ToList();

//            var dto = new ProfitLossInputDto
//            {
//                ProcurementId = vm.ProcurementId,
//                TarifAwal = vm.TarifAwal,
//                TarifAdd = vm.TarifAdd,
//                KmPer25 = vm.KmPer25,
//                SelectedVendorIds = selectedVendors,
//                Vendors = withOffers,
//            };

//            await _profitLossService.SaveInputAndCalculateAsync(dto);
//            TempData["Success"] = "Profit & Loss berhasil dihitung";

//            return RedirectToAction(nameof(Details), new { procurementId = vm.ProcurementId });
//        }

//        [HttpGet]
//        public async Task<IActionResult> Details(string procurementId)
//        {
//            var pl = await _profitLossService.GetByProcurementAsync(procurementId);
//            if (pl == null)
//                return RedirectToAction(nameof(Create), new { procurementId });

//            var allVendors = await _vendorService.GetAllVendorsAsync() ?? new List<Vendor>();
//            var selectedRows =
//                await _profitLossService.GetSelectedVendorsAsync(procurementId)
//                ?? new List<ProfitLossSelectedVendor>();
//            var offers =
//                await _voService.GetByProcurementAsync(procurementId) ?? new List<VendorOffer>();

//            var selectedNames = selectedRows
//                .Select(names =>
//                    allVendors
//                        .FirstOrDefault(vendor => vendor.VendorId == names.VendorId)
//                        ?.VendorName ?? names.VendorId
//                )
//                .ToList();

//            // map selected vendor -> final offer (jika ada)
//            var rows = offers
//                .GroupBy(offer => offer.VendorId)
//                .Select(group =>
//                {
//                    var finalOffer = group.OrderBy(x => x.Round).Last().Price;
//                    var profit = pl.Revenue - finalOffer;
//                    var profitPercent = pl.Revenue > 0 ? (profit / pl.Revenue) * 100m : 0m;
//                    var vendorName =
//                        allVendors.FirstOrDefault(v => v.VendorId == group.Key)?.VendorName
//                        ?? group.Key;
//                    var isSelected = (pl.SelectedVendorId == group.Key);
//                    return (vendorName, finalOffer, profit, profitPercent, isSelected);
//                })
//                .OrderBy(r => r.finalOffer)
//                .ToList();

//            var vm = new ProfitLossSummaryViewModel
//            {
//                ProfitLossId = pl.ProfitLossId,
//                ProcurementId = pl.ProcurementId,
//                TarifAwal = pl.TarifAwal,
//                TarifAdd = pl.TarifAdd,
//                KmPer25 = pl.KmPer25,
//                OperatorCost = pl.OperatorCost,
//                Revenue = pl.Revenue,
//                SelectedVendorId = pl.SelectedVendorId ?? "",
//                SelectedVendorName = rows.FirstOrDefault(r => r.isSelected).vendorName,
//                SelectedFinalOffer = pl.SelectedVendorFinalOffer,
//                Profit = pl.Profit,
//                ProfitPercent = pl.ProfitPercent,
//                // gunakan Rows untuk tabel (vendor berpenawaran)
//                Rows = rows.Select(r =>
//                        (r.vendorName, r.finalOffer, r.profit, r.profitPercent, r.isSelected)
//                    )
//                    .ToList(),
//            };

//            ViewBag.SelectedVendorNames = selectedNames;
//            return View(vm);
//        }

//        [HttpGet]
//        public async Task<IActionResult> Edit(string id)
//        {
//            var dto = await _profitLossService.GetEditDataAsync(id);
//            var vendors = await _vendorService.GetAllVendorsAsync();

//            var vm = new ProfitLossEditViewModel
//            {
//                ProfitLossId = dto.ProfitLossId,
//                ProcurementId = dto.ProcurementId,
//                TarifAwal = dto.TarifAwal,
//                TarifAdd = dto.TarifAdd,
//                KmPer25 = dto.KmPer25,
//                SelectedVendorIds = dto.SelectedVendorIds.ToList(),
//                Vendors = dto
//                    .Vendors.Select(vendor => new VendorOfferInputViewModel
//                    {
//                        VendorId = vendor.VendorId,
//                        Prices = vendor.Prices,
//                    })
//                    .ToList(),
//                VendorChoices = vendors
//                    .Select(vendor => new VendorChoiceViewModel
//                    {
//                        Id = vendor.VendorId,
//                        Name = vendor.VendorName,
//                    })
//                    .ToList(),
//            };

//            return View(vm);
//        }

//        // POST: /ProfitLoss/Edit/{id}
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(ProfitLossEditViewModel vm)
//        {
//            if (!ModelState.IsValid)
//            {
//                var vendors = await _vendorService.GetAllVendorsAsync();
//                vm.VendorChoices = vendors
//                    .Select(vendor => new VendorChoiceViewModel
//                    {
//                        Id = vendor.VendorId,
//                        Name = vendor.VendorName,
//                    })
//                    .ToList();

//                return View(vm);
//            }

//            var update = new ProfitLossUpdateDto
//            {
//                ProfitLossId = vm.ProfitLossId,
//                ProcurementId = vm.ProcurementId,
//                TarifAwal = vm.TarifAwal,
//                TarifAdd = vm.TarifAdd,
//                KmPer25 = vm.KmPer25,
//                SelectedVendorIds = vm.SelectedVendorIds?.Distinct().ToList() ?? [],
//                Vendors = (vm.Vendors ?? [])
//                    .Where(vendor =>
//                        vendor.Prices != null && vendor.Prices.Any(price => price > 0m)
//                    )
//                    .Select(item => new VendorOffersDto
//                    {
//                        VendorId = item.VendorId,
//                        Prices = item.Prices.Where(price => price > 0m).ToList(),
//                    })
//                    .ToList(),
//            };

//            await _profitLossService.EditProfitLossAsync(update);
//            TempData["Success"] = "Profit & Loss berhasil diupdate";

//            return RedirectToAction(
//                nameof(Index),
//                "ProfitLoss",
//                new { procurementId = vm.ProcurementId }
//            );
//        }
//    }
//}
