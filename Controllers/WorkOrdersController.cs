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
  public class WorkOrdersController : Controller {
    private readonly AppDbContext _context;

    public WorkOrdersController(AppDbContext context) {
      _context = context;
    }

    // GET: WorkOrders
    public async Task<IActionResult> Index() {
      var appDbContext = _context.WorkOrders.Include(w => w.Status).Include(w => w.Tender).Include(w => w.WoType);
      ViewBag.ActivePage = "Index Work Orders";

      return View(await appDbContext.ToListAsync());
    }

    // GET: WorkOrders/Details/5
    public async Task<IActionResult> Details(int? id) {
      if (id == null) { 
        return NotFound(); 
      }

      var workOrder = await _context.WorkOrders
        .Include(w => w.Status)
        .Include(w => w.Tender)
        .Include(w => w.WoType)
        .FirstOrDefaultAsync(m => m.WorkOrderId == id);

      if (workOrder == null) { 
        return NotFound(); 
      }

      return View(workOrder);
    }

    // GET: WorkOrders/Create
    public IActionResult Create() {
      ViewData["StatusId"] = new SelectList(_context.Statuses, "StatusId", "StatusName");
      ViewData["TenderId"] = new SelectList(_context.Tenders, "TenderId", "TenderName");
      ViewData["WoTypeId"] = new SelectList(_context.WoTypes, "WoTypeId", "TypeName");

      return View();
    }

    // POST: WorkOrders/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("WorkOrderId,WoName,Description,Note,CreatedAt,WoTypeId,StatusId,TenderId")] WorkOrder workOrder) {
      if (ModelState.IsValid) {
          _context.Add(workOrder);
          await _context.SaveChangesAsync();
          return RedirectToAction(nameof(Index));
      }

      ViewData["StatusId"] = new SelectList(_context.Statuses, "StatusId", "StatusName", workOrder.StatusId);
      ViewData["TenderId"] = new SelectList(_context.Tenders, "TenderId", "TenderName", workOrder.TenderId);
      ViewData["WoTypeId"] = new SelectList(_context.WoTypes, "WoTypeId", "TypeName", workOrder.WoTypeId);

      return View(workOrder);
    }

    // GET: WorkOrders/Edit/5
    public async Task<IActionResult> Edit(int? id) {
      if (id == null) {
        return NotFound();
      }

      var workOrder = await _context.WorkOrders.FindAsync(id);

      if (workOrder == null) {
        return NotFound();
      }
      ViewData["StatusId"] = new SelectList(_context.Statuses, "StatusId", "StatusName", workOrder.StatusId);
      ViewData["TenderId"] = new SelectList(_context.Tenders, "TenderId", "TenderName", workOrder.TenderId);
      ViewData["WoTypeId"] = new SelectList(_context.WoTypes, "WoTypeId", "TypeName", workOrder.WoTypeId);

      return View(workOrder);
    }

    // POST: WorkOrders/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("WorkOrderId,WoName,Description,Note,CreatedAt,WoTypeId,StatusId,TenderId")] WorkOrder workOrder) {
      if (id != workOrder.WorkOrderId) {
        return NotFound();
      }

      if (ModelState.IsValid) {
        try {
          _context.Update(workOrder);
          await _context.SaveChangesAsync();
        } catch (DbUpdateConcurrencyException) {
          if (!WorkOrderExists(workOrder.WorkOrderId)) {
            return NotFound();
          } else {
             throw;
          }
        }

        return RedirectToAction(nameof(Index));
      }

      ViewData["StatusId"] = new SelectList(_context.Statuses, "StatusId", "StatusName", workOrder.StatusId);
      ViewData["TenderId"] = new SelectList(_context.Tenders, "TenderId", "TenderName", workOrder.TenderId);
      ViewData["WoTypeId"] = new SelectList(_context.WoTypes, "WoTypeId", "TypeName", workOrder.WoTypeId);

      return View(workOrder);
    }

    // GET: WorkOrders/Delete/5
    public async Task<IActionResult> Delete(int? id) {
      if (id == null) {
         return NotFound();
      }

      var workOrder = await _context.WorkOrders
        .Include(w => w.Status)
        .Include(w => w.Tender)
        .Include(w => w.WoType)
        .FirstOrDefaultAsync(m => m.WorkOrderId == id);

      if (workOrder == null) {
        return NotFound();
      }

      return View(workOrder);
    }

    // POST: WorkOrders/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
      var workOrder = await _context.WorkOrders.FindAsync(id);

      if (workOrder != null) {

        _context.WorkOrders.Remove(workOrder);
      }

      await _context.SaveChangesAsync();
      return RedirectToAction(nameof(Index));
    }

    private bool WorkOrderExists(int id) {
      return _context.WorkOrders.Any(e => e.WorkOrderId == id);
    }
  }
}
