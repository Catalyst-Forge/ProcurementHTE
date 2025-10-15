using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers
{
    [Authorize]
    public class WoTypeController : Controller
    {
        private readonly IWoTypeService _woTypeService;

        public WoTypeController(IWoTypeService woTypeService)
        {
            _woTypeService = woTypeService;
        }

        // GET: WoType
        public async Task<IActionResult> Index()
        {
            var woTypes = await _woTypeService.GetAllWoTypessAsync();
            ViewBag.ActivePage = "Index Work Order Types";
            return View(woTypes);
        }

        // GET: WoType/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: WoType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TypeName,Description")] WoTypes woType)
        {
            if (!ModelState.IsValid)
                return View(woType);

            try
            {
                await _woTypeService.AddWoTypesAsync(woType);
                TempData["SuccessMessage"] = "Workorder type berhasil ditambahkan.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menambahkan data: " + ex.Message;
                return View(woType);
            }
        }

        // GET: WoType/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }
            var woType = await _woTypeService.GetWoTypesByIdAsync(id);
            return View(woType);
        }

        // POST: WoType/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int WoTypeId,
            [Bind("WoTypeId,TypeName,Description")] WoTypes woType
        )
        {
            if (WoTypeId != woType.WoTypeId)
                return NotFound();
            if (!ModelState.IsValid)
                return View(woType);

            try
            {
                await _woTypeService.EditWoTypesAsync(woType, WoTypeId);
                TempData["SuccessMessage"] = "Workorder type berhasil diupdate.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal mengupdate data: " + ex.Message;
                return View(woType);
            }
        }

        // POST: WoType/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var woType = await _woTypeService.GetWoTypesByIdAsync(id);

                if (woType == null)
                {
                    TempData["ErrorMessage"] = $"WoType dengan ID {id} tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                await _woTypeService.DeleteWoTypesAsync(woType);

                TempData["SuccessMessage"] = "Workorders Type berhasil dihapus.";
            }
            catch (DbUpdateException ex)
            {
                // tampilkan pesan SQL aslinya
                var inner = ex.InnerException?.Message ?? ex.Message;
                TempData["ErrorMessage"] = $"DBUpdateException: {inner}";
                Console.WriteLine("[DEBUG] SQL ERROR: " + inner);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
                Console.WriteLine("[DEBUG] Unexpected ERROR: " + ex);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
