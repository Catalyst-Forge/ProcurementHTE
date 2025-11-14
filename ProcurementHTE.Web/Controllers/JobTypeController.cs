using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers
{
    [Authorize]
    public class JobTypeController : Controller
    {
        private readonly IJobTypeService _jobTypeService;

        public JobTypeController(IJobTypeService jobTypeService)
        {
            _jobTypeService = jobTypeService;
        }

        // GET: JobType
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? fields = null,
            CancellationToken ct = default
        )
        {
            var allowed = new[] { 10, 25, 50, 100 };
            if (!allowed.Contains(pageSize))
                pageSize = 10;

            var selectedFields = (fields ?? "TypeName, Description")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var jobTypes = await _jobTypeService.GetAllJobTypessAsync(
                page,
                pageSize,
                search,
                selectedFields,
                ct
            );
            ViewBag.RouteData = new RouteValueDictionary
            {
                ["ActivePage"] = "Index Work Order Types",
                ["search"] = search,
                ["fields"] = string.Join(',', selectedFields),
                ["pageSize"] = pageSize,
            };

            return View(jobTypes);
        }

        // GET: JobType/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: JobType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TypeName,Description")] JobTypes jobType)
        {
            if (!ModelState.IsValid)
                return View(jobType);

            try
            {
                await _jobTypeService.AddJobTypesAsync(jobType);
                TempData["SuccessMessage"] = "Workorder type berhasil ditambahkan.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal menambahkan data: " + ex.Message;
                return View(jobType);
            }
        }

        // GET: JobType/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var jobType = await _jobTypeService.GetJobTypesByIdAsync(id);
            return View(jobType);
        }

        // POST: JobType/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string JobTypeId,
            [Bind("JobTypeId,TypeName,Description")] JobTypes jobType
        )
        {
            if (JobTypeId != jobType.JobTypeId)
                return NotFound();
            if (!ModelState.IsValid)
                return View(jobType);

            try
            {
                await _jobTypeService.EditJobTypesAsync(jobType, JobTypeId);
                TempData["SuccessMessage"] = "Workorder type berhasil diupdate.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Gagal mengupdate data: " + ex.Message;
                return View(jobType);
            }
        }

        // POST: JobType/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var jobType = await _jobTypeService.GetJobTypesByIdAsync(id);

                if (jobType == null)
                {
                    TempData["ErrorMessage"] = $"JobType dengan ID {id} tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                await _jobTypeService.DeleteJobTypesAsync(jobType);

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
