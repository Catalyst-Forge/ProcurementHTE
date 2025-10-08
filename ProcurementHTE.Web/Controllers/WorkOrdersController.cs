using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers {
  public class WorkOrdersController : Controller {
    private readonly IWorkOrderService _woService;

    public WorkOrdersController(IWorkOrderService woService) {
      _woService = woService;
    }

    // GET: WorkOrders
    public async Task<IActionResult> Index() {
      var workOrders = await _woService.GetAllWorkOrderWithDetailsAsync();
      ViewBag.ActivePage = "Index Work Orders";

      return View(workOrders);
    }

    // GET: WorkOrders/Details/5
    public async Task<IActionResult> Details(string id) {
      if (string.IsNullOrEmpty(id)) {
        return NotFound();
      }

      try {
        var workOrder = await _woService.GetWorkOrderByIdAsync(id);
        return View(workOrder);
      } catch (Exception ex) {
        TempData["ErrorMessage"] = "Gagal memuat detail Work Order: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    // GET: WorkOrders/Create
    public async Task<IActionResult> Create() {
      try {
        await PopulateDropdownsAsync();
        return View();
      } catch (Exception ex) {
        TempData["ErrorMessage"] = "Gagal memuat form: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    // POST: WorkOrders/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("WorkOrderId,WoName,Description,Note,CreatedAt,WoTypeId,StatusId,TenderId")] WorkOrder workOrder) {
      if (!ModelState.IsValid) {
        return View(workOrder);
      }

      try {
        workOrder.CreatedAt = DateTime.Now;
        await _woService.AddWorkOrderAsync(workOrder);
        TempData["SuccessMessage"] = "Work Order berhasil ditambahkan";

        return RedirectToAction(nameof(Index));
      } catch (ArgumentException ex) {
        ModelState.AddModelError("", ex.Message);
      } catch (Exception ex) {
        ModelState.AddModelError("", "Terjadi kesalahan saat menyimpan data: " + ex.Message);
      }
      await PopulateDropdownsAsync(workOrder.StatusId, workOrder.TenderId, workOrder.WoTypeId);
      return View(workOrder);
    }

    // GET: WorkOrders/Edit/5
    public async Task<IActionResult> Edit(string id) {
      if (string.IsNullOrEmpty(id))
        return NotFound();

      var workOrder = await _woService.GetWorkOrderByIdAsync(id);

      if (workOrder == null)
        return NotFound();

      try {
        await PopulateDropdownsAsync(workOrder.StatusId, workOrder.TenderId, workOrder.WoTypeId);
        return View(workOrder);
      } catch (Exception ex) {
        TempData["ErrorMessage"] = "Gagal memuat data untuk diedit: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    // POST: WorkOrders/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("WorkOrderId,WoName,Description,Note,CreatedAt,WoTypeId,StatusId,TenderId")] WorkOrder workOrder) {
      if (id != workOrder.WorkOrderId) {
        return NotFound();
      }

      try {
        if (ModelState.IsValid) {
          await _woService.EditWorkOrderAsync(workOrder, id);
          TempData["SuccessMessage"] = "Work Order berhasil diupdate!";
          return RedirectToAction(nameof(Index));
        }
      } catch (KeyNotFoundException ex) {
        ModelState.AddModelError("", ex.Message);
        return NotFound();
      } catch (Exception ex) {
        ModelState.AddModelError("", "Terjadi kesalahan saat mengupdate data: " + ex.Message);
      }

      await PopulateDropdownsAsync(workOrder.StatusId, workOrder.TenderId, workOrder.WoTypeId);
      return View(workOrder);
    }

    // GET: WorkOrders/Delete/5
    public async Task<IActionResult> Delete(string id) {
      if (string.IsNullOrEmpty(id)) {
        return NotFound();
      }

      try {
        var workOrder = await _woService.GetWorkOrderByIdAsync(id);

        if (workOrder == null) {
          return NotFound();
        }

        return View(workOrder);
      } catch (Exception ex) {
        TempData["ErrorMessage"] = "Gagal memuat data untuk delete: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    // POST: WorkOrders/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id, IFormCollection collection) {
      try {
        var workOrder = await _woService.GetWorkOrderByIdAsync(id);

        if (workOrder == null) {
          return NotFound();
        }

        await _woService.DeleteWorkOrderAsync(workOrder);
        TempData["SuccessMessage"] = "Work Order berhasil dihapus!";
        return RedirectToAction(nameof(Index));
      } catch (Exception ex) {
        TempData["ErrorMessage"] = "Gagal menghapus Work Order: " + ex.Message;
        return RedirectToAction(nameof(Index));
      }
    }

    private async Task PopulateDropdownsAsync(int? selectedStatusId = null, string? selectedTenderId = null, int? selectedWoTypeId = null) {
      var relatedEntities = await _woService.GetRelatedEntitiesForWorkOrderAsync();
      ViewData["StatusId"] = new SelectList(relatedEntities.Statuses, "StatusId", "StatusName", selectedStatusId);
      ViewData["TenderId"] = new SelectList(relatedEntities.Tenders, "TenderId", "TenderName", selectedTenderId);
      ViewData["WoTypeId"] = new SelectList(relatedEntities.WoTypes, "WoTypeId", "TypeName", selectedWoTypeId);
    }
  }
}
