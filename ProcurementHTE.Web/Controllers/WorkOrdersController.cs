using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers
{
    public class WorkOrdersController : Controller
    {
        private readonly IWorkOrderService _woService;

        public WorkOrdersController(IWorkOrderService woService)
        {
            _woService = woService;
        }

        // GET: WorkOrders
        public async Task<IActionResult> Index()
        {
            var workOrders = await _woService.GetAllWorkOrderWithDetailsAsync();
            ViewBag.TotalWo = workOrders.Count();
            ViewBag.ActivePage = "Index Work Orders";

            return View(workOrders);
        }

        // GET: WorkOrders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(id);
                return View(workOrder);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat detail Work Order: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: WorkOrders/Create
        public async Task<IActionResult> SelectType()
        {
            var related = await _woService.GetRelatedEntitiesForWorkOrderAsync();
            return View(related.WoTypes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SelectType(int woTypeId)
        {
            return RedirectToAction(nameof(CreateByType), new { woTypeId });
        }

        [HttpGet]
        public async Task<IActionResult> CreateByType(int woTypeId)
        {
            var woType = await _woService.GetWoTypeByIdAsync(woTypeId);
            if (woType is null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(selectedWoTypeId: woTypeId);

            ViewData["EnumProcurementTypes"] = Enum.GetValues(typeof(ProcurementType));
            ViewBag.SelectedWoTypeName = woType.TypeName;

            var model = new WorkOrder { WoTypeId = woTypeId };
            ViewBag.CreatePartial = ResolveCreatePartialByName(woType.TypeName);
            return View("CreateByType", model);
        }

        // POST: WorkOrders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] WorkOrder workOrder)
        {
            ModelState.Remove(nameof(WorkOrder.User));
            workOrder.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(workOrder.StatusId, workOrder.WoTypeId);
                ViewData["EnumProcurementTypes"] = Enum.GetValues(typeof(ProcurementType));

                var woTypeName = (
                    await _woService.GetWoTypeByIdAsync(workOrder.WoTypeId ?? 0)
                )?.TypeName;
                ViewBag.CreatePartial = ResolveCreatePartialByName(woTypeName);
                ViewBag.SelectedWoTypeName = woTypeName ?? "Other";

                return View("CreateByType", workOrder);
            }

            try
            {
                await _woService.AddWorkOrderAsync(workOrder);
                TempData["SuccessMessage"] = "Work Order berhasil ditambahkan";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Terjadi kesalahan saat menyimpan data: " + ex.Message
                );
            }
            await PopulateDropdownsAsync(workOrder.StatusId, workOrder.WoTypeId);

            var fallbackName = (
                await _woService.GetWoTypeByIdAsync(workOrder.WoTypeId ?? 0)
            )?.TypeName;
            ViewBag.CreatePartial = ResolveCreatePartialByName(fallbackName);
            ViewBag.SelectedWoTypeName = fallbackName ?? "Other";
            return View("CreateByType", workOrder);
        }

        // GET: WorkOrders/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var workOrder = await _woService.GetWorkOrderByIdAsync(id);

            if (workOrder == null)
                return NotFound();

            try
            {
                await PopulateDropdownsAsync(workOrder.StatusId, workOrder.WoTypeId);
                return View(workOrder);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat data untuk diedit: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkOrders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("WorkOrderId,WoName,Description,Note,CreatedAt,StatusId")] WorkOrder workOrder
        )
        {
            if (id != workOrder.WorkOrderId)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await _woService.EditWorkOrderAsync(workOrder, id);
                    TempData["SuccessMessage"] = "Work Order berhasil diupdate!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (KeyNotFoundException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return NotFound();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Terjadi kesalahan saat mengupdate data: " + ex.Message
                );
            }

            await PopulateDropdownsAsync(workOrder.StatusId, workOrder.WoTypeId);
            return View(workOrder);
        }

        // GET: WorkOrders/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(id);

                if (workOrder == null)
                {
                    return NotFound();
                }

                return View(workOrder);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal memuat data untuk delete: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, IFormCollection collection)
        {
            try
            {
                var workOrder = await _woService.GetWorkOrderByIdAsync(id);

                if (workOrder == null)
                {
                    return NotFound();
                }

                await _woService.DeleteWorkOrderAsync(workOrder);
                TempData["SuccessMessage"] = "Work Order berhasil dihapus!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menghapus Work Order: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PopulateDropdownsAsync(
            int? selectedStatusId = null,
            int? selectedWoTypeId = null
        )
        {
            var relatedEntities = await _woService.GetRelatedEntitiesForWorkOrderAsync();
            ViewData["StatusId"] = new SelectList(
                relatedEntities.Statuses,
                "StatusId",
                "StatusName",
                selectedStatusId
            );
            ViewData["WoTypeId"] = new SelectList(
                relatedEntities.WoTypes,
                "WoTypeId",
                "TypeName",
                selectedWoTypeId
            );
        }

        private static string ResolveCreatePartialByName(string? typeName)
        {
            var key = (typeName ?? "Other").Trim().ToLowerInvariant();
            return key switch
            {
                "standby" or "stand by" => "_CreateStandBy",
                "movingmobilization" or "moving mobilization" or "moving_mobilization" =>
                    "_CreateMovingMobilization",
                "spotangkutan" or "spot angkutan" => "_CreateSpotAngkutan",
                _ => "_CreateOther",
            };
        }
    }
}
