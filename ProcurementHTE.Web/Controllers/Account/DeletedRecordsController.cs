using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.Admin;

namespace ProcurementHTE.Web.Controllers.Account;

[Authorize(Roles = "Admin")]
public class DeletedRecordsController(IDeletedRecordsService deletedRecords) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DeletedRecordKind kind = DeletedRecordKind.Procurement, CancellationToken ct = default)
    {
        var model = new DeletedRecordsViewModel
        {
            Kind = kind,
            Counts = await deletedRecords.CountAsync(ct),
            Items = await deletedRecords.ListAsync(kind, ct),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(DeletedRecordKind kind, string id, CancellationToken ct = default)
    {
        var result = await deletedRecords.RestoreAsync(kind, id, ct);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { kind });
    }
}
