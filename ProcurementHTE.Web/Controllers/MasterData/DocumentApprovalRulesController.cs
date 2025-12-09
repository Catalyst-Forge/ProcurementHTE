using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Web.Controllers.MasterData;

[Authorize]
public class DocumentApprovalRulesController : Controller
{
    private readonly IDocumentApprovalRuleService _service;
    private readonly RoleManager<Role> _roleManager;
    private const string ActivePageName = "Index Document Approval Rules";

    public DocumentApprovalRulesController(
        IDocumentApprovalRuleService service,
        RoleManager<Role> roleManager
    )
    {
        _service = service;
        _roleManager = roleManager;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.ActivePage = ActivePageName;
        base.OnActionExecuting(context);
    }

    // GET: DocumentApprovalRules
    public async Task<IActionResult> Index(
        string? documentTypeId = null,
        CancellationToken ct = default
    )
    {
        var items = await _service.GetAllAsync(documentTypeId, ct);

        ViewBag.DocumentTypes = await _service.GetDocumentTypesAsync(ct);
        ViewBag.RoleMap = await _roleManager
            .Roles.Select(r => new { r.Id, r.Name })
            .ToDictionaryAsync(r => r.Id, r => r.Name ?? r.Id, ct);
        return View(items);
    }

    // GET: DocumentApprovalRules/Create
    public async Task<IActionResult> Create(CancellationToken ct = default)
    {
        await PopulateSelections(ct);
        return View(
            new DocumentApprovalRule
            {
                MinAmount = 300_000_000m,
                MaxAmount = 500_000_000m,
                Sequence = 1,
                IsActive = true,
            }
        );
    }

    // POST: DocumentApprovalRules/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        DocumentApprovalRule model,
        CancellationToken ct = default
    )
    {
        ValidateRange(model);

        if (!ModelState.IsValid)
        {
            await PopulateSelections(ct);
            return View(model);
        }

        await _service.CreateAsync(model, ct);
        TempData["SuccessMessage"] = "Approval rule created.";
        return RedirectToAction(nameof(Index));
    }

    // GET: DocumentApprovalRules/Edit/5
    public async Task<IActionResult> Edit(string id, CancellationToken ct = default)
    {
        var entity = await _service.GetByIdAsync(id, ct);
        if (entity == null)
            return NotFound();

        await PopulateSelections(ct);
        return View(entity);
    }

    // POST: DocumentApprovalRules/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        DocumentApprovalRule model,
        CancellationToken ct = default
    )
    {
        if (id != model.DocumentApprovalRuleId)
            return BadRequest();

        ValidateRange(model);

        if (!ModelState.IsValid)
        {
            await PopulateSelections(ct);
            return View(model);
        }

        await _service.UpdateAsync(model, ct);
        TempData["SuccessMessage"] = "Approval rule updated.";
        return RedirectToAction(nameof(Index));
    }

    // POST: DocumentApprovalRules/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        var entity = await _service.GetByIdAsync(id, ct);
        if (entity == null)
            return NotFound();

        await _service.DeleteAsync(id, ct);
        TempData["SuccessMessage"] = "Approval rule deleted.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateRange(DocumentApprovalRule model)
    {
        if (model.MaxAmount <= model.MinAmount)
        {
            ModelState.AddModelError(
                nameof(model.MaxAmount),
                "Max amount harus lebih besar dari Min amount."
            );
        }
    }

    private async Task PopulateSelections(CancellationToken ct)
    {
        var docTypes = await _service.GetDocumentTypesAsync(ct);
        var jobTypes = await _service.GetJobTypesAsync(ct);
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(ct);

        ViewBag.DocumentTypeSelect = new SelectList(docTypes, "DocumentTypeId", "Name");
        ViewBag.JobTypeSelect = new SelectList(jobTypes, "JobTypeId", "TypeName");
        ViewBag.RoleSelect = new SelectList(roles, "Id", "Name");
        ViewBag.Categories = Enum.GetValues(typeof(ProcurementCategory))
            .Cast<ProcurementCategory>()
            .Select(c => new SelectListItem { Text = c.ToString(), Value = ((int)c).ToString() })
            .ToList();
    }
}
