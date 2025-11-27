using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.MasterData
{
    [Authorize]
    public class VendorsController : Controller
    {
        private readonly IVendorService _vendorService;
        private const string ActivePageName = "Vendors";

        public VendorsController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.ActivePage = ActivePageName;
            base.OnActionExecuting(context);
        }

        // GET: Vendors
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
                pageSize = 25;

            var selectedFields = (fields ?? "VendorCode, VendorName, ContactPerson")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var vendors = await _vendorService.GetPagedAsync(
                page,
                pageSize,
                search,
                selectedFields,
                ct
            );
            ViewBag.RouteData = new RouteValueDictionary
            {
                ["ActivePage"] = ActivePageName,
                ["search"] = search,
                ["fields"] = string.Join(',', selectedFields),
                ["pageSize"] = pageSize,
            };
            return View(vendors);
        }

        // GET: Vendors/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return NotFound();

            var vendor = await _vendorService.GetVendorByIdAsync(id);
            return View(vendor);
        }

        // GET: Vendors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Vendors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "VendorName,NPWP,Address,City,Province,PostalCode,Email,PhoneNumber,ContactPerson,ContactPosition,Status,Comment"
            )]
                Vendor vendor
        )
        {
            ModelState.Remove(nameof(Vendor.VendorCode)); // server akan isi kode

            if (!ModelState.IsValid)
            {
                // <-- PENTING: isi lagi kalau validasi gagal
                ViewBag.Statuses = new SelectList(new[] { "Active", "Inactive", "Suspended" });
                return View(vendor);
            }

            await _vendorService.AddVendorAsync(vendor);
            TempData["SuccessMessage"] = "Vendor added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Vendors/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return NotFound();

                var vendor = await _vendorService.GetVendorByIdAsync(id);
                if (vendor == null)
                    return NotFound();

                return View(vendor);
            }
            catch (Exception ex)
            {
                // Show friendly message + go back to list
                TempData["ErrorMessage"] = "Failed to open the edit page: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Vendors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind(
                "VendorId,VendorCode,VendorName,NPWP,Address,City,Province,PostalCode,Email,PhoneNumber,ContactPerson,ContactPosition,Status,Comment"
            )]
                Vendor vendor
        )
        {
            try
            {
                if (id != vendor.VendorId)
                    return NotFound();

                // Validasi model standar
                if (!ModelState.IsValid)
                {
                    return View(vendor);
                }

                await _vendorService.EditVendorAsync(vendor, id);

                TempData["SuccessMessage"] = "Vendor updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                // Data tidak ditemukan saat update
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vendor);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Konflik concurrency (baris sudah berubah/dihapus)
                ModelState.AddModelError(
                    string.Empty,
                    "Vendor data changed while you were editing. Please refresh and try again."
                );
                return View(vendor);
            }
            catch (DbUpdateException ex)
            {
                // Error DB (unique constraint, dsb.)
                ModelState.AddModelError(
                    string.Empty,
                    "Failed to save changes to the database. " + ex.GetBaseException().Message
                );
                return View(vendor);
            }
            catch (Exception ex)
            {
                // Fallback umum
                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred: " + ex.Message
                );
                return View(vendor);
            }
        }

        // POST: Vendors/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var vendor = await _vendorService.GetVendorByIdAsync(id);
                if (vendor == null)
                {
                    return RedirectToAction(nameof(Index));
                }
                await _vendorService.DeleteVendorAsync(vendor);

                TempData["SuccessMessage"] = "Vendor deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete vendor: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
