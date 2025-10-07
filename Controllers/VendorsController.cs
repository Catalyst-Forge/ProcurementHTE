using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using project_25_07.Data;
using project_25_07.Models;

namespace project_25_07.Controllers {
  public class VendorsController : Controller {
    private readonly AppDbContext _context;

    public VendorsController(AppDbContext context) {
      _context = context;
    }

    // GET: Vendors
    public async Task<IActionResult> Index() {
      var appDbContext = _context.Vendors.Include(v => v.User);
      ViewBag.ActivePage = "Vendors";
      return View(await appDbContext.ToListAsync());
    }

    // GET: Vendors/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) {
        return NotFound();
      }

      var vendor = await _context.Vendors
        .Include(v => v.User)
        .FirstOrDefaultAsync(m => m.VendorId == id);

      if (vendor == null) {
        return NotFound();
      }

      return View(vendor);
    }

    // GET: Vendors/Create
    public IActionResult Create() {
      ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName");
      return View();
    }

    // POST: Vendors/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("VendorId,VendorName,Price,Documents,CreatedAt,UserId")] Vendor vendor) {
      if (ModelState.IsValid) {
        _context.Add(vendor);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }

      ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", vendor.UserId);
      return View(vendor);
    }

    // GET: Vendors/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) {
        return NotFound();
      }

      var vendor = await _context.Vendors.FindAsync(id);

      if (vendor == null) {
        return NotFound();
      }

      ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", vendor.UserId);
      return View(vendor);
    }

    // POST: Vendors/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("VendorId,VendorName,Price,Documents,CreatedAt,UserId")] Vendor vendor) {
      if (id != vendor.VendorId) {
        return NotFound();
      }

      if (ModelState.IsValid) {
        try {
          _context.Update(vendor);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!VendorExists(vendor.VendorId)) {
            return NotFound();
          } else {
            throw;
          }
        }
        return RedirectToAction(nameof(Index));
      }
      ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", vendor.UserId);
      return View(vendor);
    }

    // GET: Vendors/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      if (id == null) {
        return NotFound();
      }

      var vendor = await _context.Vendors
        .Include(v => v.User)
        .FirstOrDefaultAsync(m => m.VendorId == id);
      if (vendor == null) {
        return NotFound();
      }

      return View(vendor);
    }

    // POST: Vendors/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      var vendor = await _context.Vendors.FindAsync(id);

      if (vendor != null) {
        _context.Vendors.Remove(vendor);
      }

      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    private bool VendorExists(int id) {
      return _context.Vendors.Any(e => e.VendorId == id);
    }
  }
}
