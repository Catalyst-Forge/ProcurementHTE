using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Web.Controllers.MasterData;

[Authorize]
public class JobTypeDocumentController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<JobTypeDocumentController> _logger;
    private const string ActivePageName = "Index Job Type Documents";

    public JobTypeDocumentController(AppDbContext db, ILogger<JobTypeDocumentController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        ViewBag.ActivePage = ActivePageName;
        base.OnActionExecuting(context);
    }

    // GET: JobTypeDocument
    public async Task<IActionResult> Index(string? jobTypeId = null, CancellationToken ct = default)
    {
        var query = _db.JobTypeDocuments
            .Include(x => x.JobType)
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
        return View(new JobTypeDocuments { IsMandatory = true, IsUploadRequired = true, Sequence = 1 });
    }

    // POST: JobTypeDocument/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobTypeDocuments model, CancellationToken ct = default)
    {
        if (await ExistsAsync(model.JobTypeId, model.DocumentTypeId, ct))
        {
            ModelState.AddModelError(string.Empty, "Mapping for this Job Type and Document Type already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelections(ct);
            return View(model);
        }

        _db.JobTypeDocuments.Add(model);
        await _db.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Job Type Document mapping created.";
        return RedirectToAction(nameof(Index));
    }

    // GET: JobTypeDocument/Edit/5
    public async Task<IActionResult> Edit(string id, CancellationToken ct = default)
    {
        var entity = await _db.JobTypeDocuments.FindAsync(new object?[] { id }, ct);
        if (entity is null) return NotFound();
        await PopulateSelections(ct);
        return View(entity);
    }

    // POST: JobTypeDocument/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, JobTypeDocuments model, CancellationToken ct = default)
    {
        if (id != model.JobTypeDocumentId) return BadRequest();

        if (await ExistsAsync(model.JobTypeId, model.DocumentTypeId, ct, excludeId: id))
        {
            ModelState.AddModelError(string.Empty, "Mapping for this Job Type and Document Type already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelections(ct);
            return View(model);
        }

        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Job Type Document mapping updated.";
        return RedirectToAction(nameof(Index));
    }

    // POST: JobTypeDocument/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        var entity = await _db.JobTypeDocuments.FindAsync(new object?[] { id }, ct);
        if (entity is null) return NotFound();

        _db.JobTypeDocuments.Remove(entity);
        await _db.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Mapping deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelections(CancellationToken ct)
    {
        ViewBag.JobTypes = await _db.JobTypes.OrderBy(x => x.TypeName).ToListAsync(ct);
        ViewBag.DocumentTypes = await _db.DocumentTypes.OrderBy(x => x.Name).ToListAsync(ct);
    }

    private Task<bool> ExistsAsync(string jobTypeId, string documentTypeId, CancellationToken ct, string? excludeId = null)
    {
        var query = _db.JobTypeDocuments.AsQueryable();
        query = query.Where(x => x.JobTypeId == jobTypeId && x.DocumentTypeId == documentTypeId);
        if (!string.IsNullOrWhiteSpace(excludeId))
        {
            query = query.Where(x => x.JobTypeDocumentId != excludeId);
        }
        return query.AnyAsync(ct);
    }
}
