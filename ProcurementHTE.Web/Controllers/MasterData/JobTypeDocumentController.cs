using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Web.Controllers.MasterData;

[Authorize]
public class JobTypeDocumentController : Controller
{
    private readonly AppDbContext _db;
    private const string ActivePageName = "Index Job Type Documents";

    public JobTypeDocumentController(AppDbContext db)
    {
        _db = db;
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
        var query = _db
            .JobTypeDocuments.Include(x => x.JobType)
            .Include(x => x.DocumentType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(jobTypeId))
        {
            query = query.Where(x => x.JobTypeId == jobTypeId);
        }

        var items = await query
            .OrderBy(x => x.JobType.TypeName)
            .ThenBy(x => x.Sequence)
            .ToListAsync(ct);

        ViewBag.JobTypes = await _db.JobTypes.OrderBy(x => x.TypeName).ToListAsync(ct);
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
            await ExistsAsync(
                model.JobTypeId,
                model.DocumentTypeId,
                model.ProcurementCategory,
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
            _db.JobTypeDocuments.Add(model);
            await _db.SaveChangesAsync(ct);
            TempData["SuccessMessage"] = "Job Type Document mapping created.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            // Surface DB errors (e.g., FK violations) to the UI instead of a blank loading state.
            var detail = ex.InnerException?.Message ?? ex.Message;
            ModelState.AddModelError(string.Empty, $"Failed to save mapping: {detail}");
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
        var entity = await _db.JobTypeDocuments.FindAsync(new object?[] { id }, ct);
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
            await ExistsAsync(
                model.JobTypeId,
                model.DocumentTypeId,
                model.ProcurementCategory,
                ct,
                excludeId: id
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
            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync(ct);
            TempData["SuccessMessage"] = "Job Type Document mapping updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            ModelState.AddModelError(string.Empty, $"Failed to update mapping: {detail}");
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
        var entity = await _db.JobTypeDocuments.FindAsync(new object?[] { id }, ct);
        if (entity is null)
            return NotFound();

        _db.JobTypeDocuments.Remove(entity);
        await _db.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Mapping deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelections(CancellationToken ct)
    {
        ViewBag.JobTypes = await _db.JobTypes.OrderBy(x => x.TypeName).ToListAsync(ct);
        ViewBag.DocumentTypes = await _db.DocumentTypes.OrderBy(x => x.Name).ToListAsync(ct);
        ViewBag.Categories =
            new List<SelectListItem>
            {
                new("All (Goods & Services)", ""),
                new("Goods", ((int)ProcurementHTE.Core.Enums.ProcurementCategory.Goods).ToString()),
                new("Services", ((int)ProcurementHTE.Core.Enums.ProcurementCategory.Services).ToString()),
            };
    }

    private Task<bool> ExistsAsync(
        string jobTypeId,
        string documentTypeId,
        ProcurementHTE.Core.Enums.ProcurementCategory? procurementCategory,
        CancellationToken ct,
        string? excludeId = null
    )
    {
        var query = _db.JobTypeDocuments.AsQueryable();
        query = query.Where(x => x.JobTypeId == jobTypeId && x.DocumentTypeId == documentTypeId);
        if (procurementCategory.HasValue)
        {
            query = query.Where(x => x.ProcurementCategory == procurementCategory);
        }
        else
        {
            query = query.Where(x => x.ProcurementCategory == null);
        }
        if (!string.IsNullOrWhiteSpace(excludeId))
        {
            query = query.Where(x => x.JobTypeDocumentId != excludeId);
        }
        return query.AnyAsync(ct);
    }

    private void LogModelErrors(string actionName)
    {
        if (ModelState.IsValid)
            return;

        var errors = ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .Select(
                kvp =>
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
