using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.MasterData;

[Authorize]
public class JobTypeDocumentController : Controller
{
    private readonly IJobTypeDocumentAdminService _service;
    private const string ActivePageName = "Index Job Type Documents";

    public JobTypeDocumentController(IJobTypeDocumentAdminService service)
    {
        _service = service;
    }

    public override void OnActionExecuting(
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context
    )
    {
        ViewBag.ActivePage = ActivePageName;
        base.OnActionExecuting(context);
    }

    // GET: JobTypeDocument
    public async Task<IActionResult> Index(string? jobTypeId = null, CancellationToken ct = default)
    {
        var items = await _service.GetAllAsync(jobTypeId, ct);
        ViewBag.JobTypes = await _service.GetJobTypesAsync(ct);
        return View(items);
    }

    // GET: JobTypeDocument/Create
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        await PopulateSelections(ct);
        return View(
            new JobTypeDocuments
            {
                IsMandatory = true,
                IsUploadRequired = true,
                Sequence = 1,
            }
        );
    }

    // POST: JobTypeDocument/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobTypeDocuments model, CancellationToken ct = default)
    {
        if (model.Sequence <= 0)
        {
            model.Sequence = 1;
            ModelState.Remove(nameof(model.Sequence));
        }

        if (
            await _service.ExistsAsync(
                model.JobTypeId,
                model.DocumentTypeId,
                model.ProcurementCategory,
                excludeId: null,
                ct
            )
        )
        {
            ModelState.AddModelError(
                string.Empty,
                "Mapping for this Job Type and Document Type already exists."
            );
        }

        if (!ModelState.IsValid)
        {
            LogModelErrors("Create");
            await PopulateSelections(ct);
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.JobTypeDocumentId))
        {
            model.JobTypeDocumentId = Guid.NewGuid().ToString();
        }

        try
        {
            await _service.CreateAsync(model, ct);
            TempData["SuccessMessage"] = "Job Type Document mapping created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Failed to save mapping: {ex.Message}");
        }

        await PopulateSelections(ct);
        return View(model);
    }

    // GET: JobTypeDocument/Edit/5
    public async Task<IActionResult> Edit(string id, CancellationToken ct = default)
    {
        var entity = await _service.GetByIdAsync(id, ct);
        if (entity is null)
            return NotFound();
        await PopulateSelections(ct);
        return View(entity);
    }

    // POST: JobTypeDocument/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        JobTypeDocuments model,
        CancellationToken ct = default
    )
    {
        if (model.Sequence <= 0)
        {
            model.Sequence = 1;
            ModelState.Remove(nameof(model.Sequence));
        }

        if (id != model.JobTypeDocumentId)
            return BadRequest();

        if (
            await _service.ExistsAsync(
                model.JobTypeId,
                model.DocumentTypeId,
                model.ProcurementCategory,
                excludeId: id,
                ct
            )
        )
        {
            ModelState.AddModelError(
                string.Empty,
                "Mapping for this Job Type and Document Type already exists."
            );
        }

        if (!ModelState.IsValid)
        {
            LogModelErrors("Edit");
            await PopulateSelections(ct);
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.JobTypeDocumentId))
        {
            model.JobTypeDocumentId = id;
        }

        try
        {
            await _service.UpdateAsync(model, ct);
            TempData["SuccessMessage"] = "Job Type Document mapping updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Failed to update mapping: {ex.Message}");
        }

        await PopulateSelections(ct);
        return View(model);
    }

    // POST: JobTypeDocument/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct);
        TempData["SuccessMessage"] = "Mapping deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelections(CancellationToken ct)
    {
        ViewBag.JobTypes = await _service.GetJobTypesAsync(ct);
        ViewBag.DocumentTypes = await _service.GetDocumentTypesAsync(ct);
        ViewBag.Categories = new List<SelectListItem>
        {
            new("All (Barang & Jasa)", ""),
            new("Barang", ((int)ProcurementCategory.Barang).ToString()),
            new("Jasa", ((int)ProcurementCategory.Jasa).ToString()),
        };
    }

    private void LogModelErrors(string actionName)
    {
        if (ModelState.IsValid)
            return;

        var errors = ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .Select(kvp =>
                $"{kvp.Key}: {string.Join(" | ", kvp.Value!.Errors.Select(e => e.ErrorMessage ?? e.Exception?.Message ?? "<no message>"))}"
            )
            .ToArray();

        if (errors.Length > 0)
        {
            Console.WriteLine(
                $"[JobTypeDocumentController:{actionName}] ModelState invalid ({HttpContext.TraceIdentifier}): {string.Join("; ", errors)}"
            );
        }
    }
}
