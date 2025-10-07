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
  public class TendersController : Controller {
    private readonly AppDbContext _context;

    public TendersController(AppDbContext context) {
      _context = context;
    }

    // GET: Tenders
    public async Task<IActionResult> Index() {
      ViewBag.ActivePage = "Tenders";
      return View(await _context.Tenders.ToListAsync());
    }

    // GET: Tenders/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) {
        return NotFound();
      }

      var tender = await _context.Tenders
        .FirstOrDefaultAsync(m => m.TenderId == id);

      if (tender == null) {
        return NotFound();
      }

      return View(tender);
    }

    // GET: Tenders/Create
    public IActionResult Create() {
      return View();
    }

    // POST: Tenders/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TenderId,TenderName,Price,Information,CreatedAt")] Tender tender) {
      if (ModelState.IsValid) {
        _context.Add(tender);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
      }
      return View(tender);
    }

    // GET: Tenders/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) {
        return NotFound();
      }

      var tender = await _context.Tenders.FindAsync(id);

      if (tender == null) {
        return NotFound();
      }

      return View(tender);
    }

    // POST: Tenders/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("TenderId,TenderName,Price,Information,CreatedAt")] Tender tender) {
      if (id != tender.TenderId) {
        return NotFound();
      }

      if (ModelState.IsValid) {
        try {
          _context.Update(tender);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!TenderExists(tender.TenderId)) {
            return NotFound();
          } else {
            throw;
          }
        }

        return RedirectToAction(nameof(Index));
      }

      return View(tender);
    }

    // GET: Tenders/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      if (id == null) {
        return NotFound();
      }

      var tender = await _context.Tenders
        .FirstOrDefaultAsync(m => m.TenderId == id);
      if (tender == null) {
        return NotFound();
      }

      return View(tender);
    }

    // POST: Tenders/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      var tender = await _context.Tenders.FindAsync(id);

      if (tender != null) {
        _context.Tenders.Remove(tender);
      }

      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    private bool TenderExists(int id) {
      return _context.Tenders.Any(e => e.TenderId == id);
    }
  }
}
