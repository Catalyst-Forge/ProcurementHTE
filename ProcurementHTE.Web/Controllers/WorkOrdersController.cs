using System.ComponentModel.Design;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Authorization;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers
{
    [Authorize]
    public class WorkOrdersController : Controller
    {
        private readonly IWorkOrderService _woService;
        private readonly ILogger<WorkOrder> _logger;

        public WorkOrdersController(IWorkOrderService woService, ILogger<WorkOrder> logger)
        {
            _woService = woService;
            _logger = logger;
        }

        // GET: WorkOrders
        [Authorize(Policy = Permissions.WO.Read)]
        public async Task<IActionResult> Index()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var hasClaim = User.HasClaim("permission", Permissions.WO.Read);

            _logger.LogInformation("", $"User punya claim {hasClaim}");
            
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

            var createWoVM = new WorkOrderCreateViewModel
            {
                WorkOrder = new WorkOrder { WoTypeId = woTypeId },
            };
            ViewBag.CreatePartial = ResolveCreatePartialByName(woType.TypeName);
            return View("CreateByType", createWoVM);
        }

        // POST: WorkOrders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromForm] WorkOrderCreateViewModel woViewModel,
            string submitAction
        )
        {
            ModelState.Remove(nameof(User));
            woViewModel ??= new WorkOrderCreateViewModel();
            woViewModel.WorkOrder.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(submitAction))
            {
                var status = await _woService.GetStatusByNameAsync(submitAction);
                if (status == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Status '{submitAction}' tidak ditemukan. Pastikan entries di table Statuses ada."
                    );
                }
                else
                {
                    woViewModel.WorkOrder.StatusId = status.StatusId;
                }
            }
            else
            {
                ModelState.AddModelError("", "Aksi submit tidak dikenali");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(
                    woViewModel.WorkOrder.StatusId,
                    woViewModel.WorkOrder.WoTypeId
                );
                ViewData["EnumProcurementTypes"] = Enum.GetValues(typeof(ProcurementType));

                var woTypeName = (
                    await _woService.GetWoTypeByIdAsync(woViewModel.WorkOrder.WoTypeId ?? 0)
                )?.TypeName;
                ViewBag.CreatePartial = ResolveCreatePartialByName(woTypeName);
                ViewBag.SelectedWoTypeName = woTypeName ?? "Other";

                return View("CreateByType", woViewModel);
            }

            try
            {
                //await _woService.AddWorkOrderAsync(workOrder);
                await _woService.AddWorkOrderWithDetailsAsync(
                    woViewModel.WorkOrder,
                    woViewModel.Details
                );
                TempData["SuccessMessage"] = submitAction.Equals(
                    "Draft",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Work Order berhasil disimpan sebagai draft"
                    : "Work Order berhasil dibuat";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menyimpan Work Order");
                ModelState.AddModelError(
                    "",
                    "Terjadi kesalahan saat menyimpan data: " + ex.Message
                );
            }
            await PopulateDropdownsAsync(
                woViewModel.WorkOrder.StatusId,
                woViewModel.WorkOrder.WoTypeId
            );

            var fallbackName = (
                await _woService.GetWoTypeByIdAsync(woViewModel.WorkOrder.WoTypeId ?? 0)
            )?.TypeName;
            ViewBag.CreatePartial = ResolveCreatePartialByName(fallbackName);
            ViewBag.SelectedWoTypeName = fallbackName ?? "Other";
            return View("CreateByType", woViewModel);
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
        public async Task<IActionResult> Edit(WorkOrder workOrder, string id)
        {
            ModelState.Remove(nameof(WorkOrder.User));

            if (id != workOrder.WorkOrderId)
            {
                return NotFound();
            }

            try
            {
                _logger.LogInformation("kesalahan");
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
                "standby" or "stand by" or "stand_by" => "_CreateStandBy",
                "movingmobilization"
                or "moving mobilization"
                or "moving_mobilization"
                or "moving & mobilization"
                or "moving and mobilization"
                or "moving dan mobilization" => "_CreateMovingMobilization",
                "spotangkutan"
                or "spot angkutan"
                or "spot_angkutan"
                or "spot & angkutan"
                or "spot and angkutan"
                or "spot dan angkutan" => "_CreateSpotAngkutan",
                _ => "_CreateOther",
            };
        }
    }
}
