using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Extensions;

namespace ProcurementHTE.Web.Controllers.MasterData;

[Authorize]
public class DocumentApprovalsController : Controller
{
    private readonly IDocumentApprovalsService _service;
    private readonly ILogger<DocumentApprovalsController> _logger;
    private const string ActivePageName = "Index Document Approvals";

    public DocumentApprovalsController(
        IDocumentApprovalsService service,
        ILogger<DocumentApprovalsController> logger
    )
    {
        _service = service;
        _logger = logger;
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
        var list = await _service.GetAllAsync(jobTypeId, ct);

        var jobTypeDocs = await _service.GetJobTypeDocumentsAsync(ct);
        ViewBag.JobTypes = jobTypeDocs
            .Select(j => j.JobType)
            .Where(j => j != null)
            .GroupBy(j => j!.JobTypeId)
            .Select(g => g.First()!)
            .OrderBy(j => j.TypeName)
            .ToList();
        return View(list);
    }

    // GET: DocumentApprovals/Create
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        await PopulateSelections(ct);
        return View(new DocumentApprovals { Level = 1 });
    }

    // POST: DocumentApprovals/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentApprovals model, CancellationToken ct = default)
    {
        if (model.Level <= 0)
        {
            ModelState.Remove(nameof(model.Level));
            model.Level = 1;
        }

        if (!ModelState.IsValid)
        {
            _logger.LogInvalidModelState(
                ModelState,
                "document approvals",
                "Create",
                HttpContext.TraceIdentifier
            );
            await PopulateSelections(ct);
            return View(model);
        }

        await _service.CreateAsync(model, ct);
        TempData["SuccessMessage"] = "Approval step created.";
        return RedirectToAction(nameof(Index));
    }

    // GET: DocumentApprovals/Edit/5
    public async Task<IActionResult> Edit(string id, CancellationToken ct = default)
    {
        var entity = await _service.GetByIdAsync(id, ct);
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
        if (model.Level <= 0)
        {
            ModelState.Remove(nameof(model.Level));
            model.Level = 1;
        }

        if (id != model.DocumentApprovalId)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            _logger.LogInvalidModelState(
                ModelState,
                "document approvals",
                "Edit",
                HttpContext.TraceIdentifier
            );
            await PopulateSelections(ct);
            return View(model);
        }

        await _service.UpdateAsync(model, ct);
        TempData["SuccessMessage"] = "Approval step updated.";
        return RedirectToAction(nameof(Index));
    }

    // POST: DocumentApprovals/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct);
        TempData["SuccessMessage"] = "Approval step deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelections(CancellationToken ct)
    {
        var jobTypeDocs = await _service.GetJobTypeDocumentsAsync(ct);
        var roles = await _service.GetRolesAsync(ct);

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
