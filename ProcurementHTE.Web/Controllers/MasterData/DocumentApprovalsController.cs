using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Web.Controllers.MasterData;

[Authorize]
public class DocumentApprovalsController : Controller
{
    private readonly AppDbContext _db;
    private readonly RoleManager<Role> _roleManager;
    private const string ActivePageName = "Index Document Approvals";

    public DocumentApprovalsController(AppDbContext db, RoleManager<Role> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public override void OnActionExecuting(
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context
    )
    {
        ViewBag.ActivePage = ActivePageName;
        base.OnActionExecuting(context);
    }

    // GET: DocumentApprovals
    public async Task<IActionResult> Index(string? jobTypeId = null, CancellationToken ct = default)
    {
        var approvals = _db
            .DocumentApprovals.Include(x => x.JobTypeDocument)
            .ThenInclude(j => j.JobType)
            .Include(x => x.JobTypeDocument)
            .ThenInclude(j => j.DocumentType)
            .Include(x => x.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(jobTypeId))
        {
            approvals = approvals.Where(x => x.JobTypeDocument.JobTypeId == jobTypeId);
        }

        var list = await approvals
            .OrderBy(x => x.JobTypeDocument.JobType.TypeName)
            .ThenBy(x => x.JobTypeDocument.DocumentType.Name)
            .ThenBy(x => x.Level)
            .ThenBy(x => x.SequenceOrder)
            .ToListAsync(ct);

        ViewBag.JobTypes = await _db.JobTypes.OrderBy(x => x.TypeName).ToListAsync(ct);
        return View(list);
    }

    // GET: DocumentApprovals/Create
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        await PopulateSelections(ct);
        return View(new DocumentApprovals { Level = 1, SequenceOrder = 1 });
    }

    // POST: DocumentApprovals/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentApprovals model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelections(ct);
            return View(model);
        }

        _db.DocumentApprovals.Add(model);
        await _db.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Approval step created.";
        return RedirectToAction(nameof(Index));
    }

    // GET: DocumentApprovals/Edit/5
    public async Task<IActionResult> Edit(string id, CancellationToken ct = default)
    {
        var entity = await _db.DocumentApprovals.FindAsync(new object?[] { id }, ct);
        if (entity is null)
            return NotFound();
        await PopulateSelections(ct);
        return View(entity);
    }

    // POST: DocumentApprovals/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        DocumentApprovals model,
        CancellationToken ct = default
    )
    {
        if (id != model.DocumentApprovalId)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateSelections(ct);
            return View(model);
        }

        _db.Entry(model).State = EntityState.Modified;
        await _db.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Approval step updated.";
        return RedirectToAction(nameof(Index));
    }

    // POST: DocumentApprovals/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        var entity = await _db.DocumentApprovals.FindAsync(new object?[] { id }, ct);
        if (entity is null)
            return NotFound();

        _db.DocumentApprovals.Remove(entity);
        await _db.SaveChangesAsync(ct);
        TempData["SuccessMessage"] = "Approval step deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelections(CancellationToken ct)
    {
        var jobTypeDocs = await _db
            .JobTypeDocuments.Include(x => x.JobType)
            .Include(x => x.DocumentType)
            .OrderBy(x => x.JobType.TypeName)
            .ThenBy(x => x.DocumentType.Name)
            .ToListAsync(ct);

        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(ct);

        ViewBag.JobTypeDocuments = jobTypeDocs;
        ViewBag.JobTypeDocumentSelect = jobTypeDocs
            .Select(x => new
            {
                x.JobTypeDocumentId,
                Display = $"{x.JobType?.TypeName} - {x.DocumentType?.Name}",
            })
            .ToList();
        ViewBag.Roles = roles;
    }
}
