using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers
{
    [Authorize]
    public class TendersController : Controller
    {
        private readonly ITenderService _tenderService;

        public TendersController(ITenderService tenderService)
        {
            _tenderService = tenderService;
        }

        // GET: Tenders
        public async Task<IActionResult> Index()
        {
            var tenders = await _tenderService.GetAllTenderAsync();
            ViewBag.ActivePage = "Tenders";

            return View(tenders);
        }

        // GET: Tenders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var tender = await _tenderService.GetTenderByIdAsync(id);

            return View(tender);
        }

        // GET: Tenders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tenders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("TenderName,Price,Information")] Tender tender
        )
        {
            if (!ModelState.IsValid)
            {
                return View(tender);
            }

            try
            {
                tender.CreatedAt = DateTime.Now;
                await _tenderService.AddTenderAsync(tender);
                TempData["SuccessMessage"] = "Tender added successfully."; // Notification/Toast Message for data success

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", ex.Message);

                return View(tender);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An error occurred while saving the data: " + ex.Message
                );

                return View(tender);
            }
        }

        // GET: Tenders/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var tender = await _tenderService.GetTenderByIdAsync(id);

            return View(tender);
        }

        // POST: Tenders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            [Bind("TenderId,TenderName,Price,Information,CreatedAt")] Tender tender
        )
        {
            if (id != tender.TenderId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(tender);
            }

            try
            {
                await _tenderService.EditTenderAsync(tender, id);
                TempData["SuccessMessage"] = "Tender updated successfully."; // Notification/Toast Message for data success

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An issue occurred while updating the data: " + ex.Message
                );

                return View(tender);
            }
        }

        // GET: Tenders/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var tender = await _tenderService.GetTenderByIdAsync(id);

            return View(tender);
        }

        // POST: Tenders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var tender = await _tenderService.GetTenderByIdAsync(id);
                if (tender == null)
                {
                    return NotFound();
                }
                await _tenderService.DeleteTenderAsync(tender);
                TempData["SuccessMessage"] = "Tender deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete tender: " + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }
    }
}
