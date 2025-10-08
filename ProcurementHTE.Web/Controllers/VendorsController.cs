using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers {
  public class VendorsController : Controller {
    private readonly IVendorService _vendorService;

    public VendorsController(IVendorService vendorService) {
      _vendorService = vendorService;
    }

    // GET: Vendors
    public async Task<IActionResult> Index() {
      var vendors = await _vendorService.GetAllVendorsAsync();
      ViewBag.ActivePage = "Vendors";
      return View(vendors);
    }

    // GET: Vendors/Details/5
    public async Task<IActionResult> Details(string id) {
      if (id == null)
        return NotFound();


      var vendor = await _vendorService.GetVendorByIdAsync(id);
      return View(vendor);
    }

    // GET: Vendors/Create
    public IActionResult Create() {
      //ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName");
      return View();
    }

    // POST: Vendors/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("VendorId,VendorName,Price,Documents,CreatedAt,UserId")] Vendor vendor) {
      try {
        await _vendorService.AddVendorAsync(vendor);
        return RedirectToAction(nameof(Index));
      } catch {
        return View();
      }
    }

    // GET: Vendors/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      return View();
    }

    // POST: Vendors/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("VendorId,VendorName,Price,Documents,CreatedAt,UserId")] Vendor vendor) {
      //
      return View();
    }

    // GET: Vendors/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      //if (id == null) {
      //  return NotFound();
      //}

      //var vendor = await _context.Vendors
      //  .Include(v => v.User)
      //  .FirstOrDefaultAsync(m => m.VendorId == id);
      //if (vendor == null) {
      //  return NotFound();
      //}

      //return View(vendor);
      return View();
    }

    // POST: Vendors/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      //var vendor = await _context.Vendors.FindAsync(id);

      //if (vendor != null) {
      //  _context.Vendors.Remove(vendor);
      //}

      //await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    //private bool VendorExists(int id) {
    //  return _context.Vendors.Any(e => e.VendorId == id);
    //}
  }
}
