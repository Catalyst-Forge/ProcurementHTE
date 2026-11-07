using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.Admin;

namespace ProcurementHTE.Web.Controllers.Account;

[Authorize(Roles = "Admin")]
public class DeletedRecordsController(IDeletedRecordsService deletedRecords, IRecordPurgeService purge) : Controller
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

    [HttpGet]
    public async Task<IActionResult> Purge(DeletedRecordKind kind, string id, CancellationToken ct = default)
    {
        var preview = await purge.PreviewAsync(kind, id, ct);
        if (preview is null)
            return RecordGone(kind);

        return View(new PurgePageViewModel { Preview = preview, Form = new PurgeInputModel { Kind = kind, Id = id } });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Purge([Bind(Prefix = "Form")] PurgeInputModel form, CancellationToken ct = default)
    {
        if (form.Confirmation != PurgeInputModel.ConfirmationText)
            ModelState.AddModelError("Form.Confirmation", $"Ketik {PurgeInputModel.ConfirmationText} untuk mengonfirmasi.");

        if (ModelState.IsValid)
        {
            var result = await purge.PurgeAsync(
                new PurgeRequest(
                    form.Kind,
                    form.Id,
                    form.Reason,
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                ),
                ct
            );
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index), new { kind = form.Kind });
            }

            ModelState.AddModelError(string.Empty, result.Message);
        }

        var preview = await purge.PreviewAsync(form.Kind, form.Id, ct);
        return preview is null ? RecordGone(form.Kind) : View(new PurgePageViewModel { Preview = preview, Form = form });
    }

    [HttpGet]
    public async Task<IActionResult> History(CancellationToken ct = default) =>
        View(await purge.ListLogAsync(ct));

    private RedirectToActionResult RecordGone(DeletedRecordKind kind)
    {
        TempData["ErrorMessage"] = "Data tidak ditemukan, atau belum ada di Data Terhapus.";
        return RedirectToAction(nameof(Index), new { kind });
    }
}
