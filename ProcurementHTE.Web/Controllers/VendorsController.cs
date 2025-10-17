using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers
{
    [Authorize]
    public class VendorsController : Controller
    {
        private readonly IVendorService _vendorService;

        public VendorsController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        // GET: Vendors
        public async Task<IActionResult> Index()
        {
            var vendors = await _vendorService.GetAllVendorsAsync();
            ViewBag.ActivePage = "Vendors";
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
            ViewBag.Statuses = new SelectList(new[] { "Active", "Inactive", "Suspended" });
            return View();
        }

        // POST: Vendors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
          [Bind("VendorName,NPWP,Address,City,Province,PostalCode,Email,PhoneNumber,ContactPerson,ContactPosition,Status,Comment")]
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
            TempData["SuccessMessage"] = "Vendor berhasil ditambahkan.";
            return RedirectToAction(nameof(Index));
        }



        // Helper untuk isi dropdown status
        private void BindStatuses(string? selected = null)
        {
            ViewBag.Statuses = new SelectList(
                new[] { "Active", "Inactive", "Suspended" }, selected
            );
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

                BindStatuses(vendor.Status);
                return View(vendor);
            }
            catch (Exception ex)
            {
                // Tampilkan pesan ramah + kembali ke list
                TempData["ErrorMessage"] = "Gagal membuka halaman edit vendor: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Vendors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("VendorId,VendorCode,VendorName,NPWP,Address,City,Province,PostalCode,Email,PhoneNumber,ContactPerson,ContactPosition,Status,Comment")]
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
                    BindStatuses(vendor.Status);
                    return View(vendor);
                }

                await _vendorService.EditVendorAsync(vendor, id);

                TempData["SuccessMessage"] = "Vendor berhasil diupdate.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                // Data tidak ditemukan saat update
                ModelState.AddModelError(string.Empty, ex.Message);
                BindStatuses(vendor.Status);
                return View(vendor);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Konflik concurrency (baris sudah berubah/dihapus)
                ModelState.AddModelError(string.Empty,
                    "Data vendor berubah saat Anda mengedit. Silakan muat ulang halaman dan coba lagi.");
                BindStatuses(vendor.Status);
                return View(vendor);
            }
            catch (DbUpdateException ex)
            {
                // Error DB (unique constraint, dsb.)
                ModelState.AddModelError(string.Empty,
                    "Gagal menyimpan perubahan ke database. " + ex.GetBaseException().Message);
                BindStatuses(vendor.Status);
                return View(vendor);
            }
            catch (Exception ex)
            {
                // Fallback umum
                ModelState.AddModelError(string.Empty, "Terjadi kesalahan tak terduga: " + ex.Message);
                BindStatuses(vendor.Status);
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

                TempData["SuccessMessage"] = "Vendor berhasil dihapus.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menghapus data: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

