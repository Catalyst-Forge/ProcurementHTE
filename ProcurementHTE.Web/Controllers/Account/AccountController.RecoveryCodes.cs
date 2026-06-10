using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AccountController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        var codes = await _accountService.GenerateRecoveryCodesAsync(
            user.Id,
            HttpContext.RequestAborted
        );

        TempData["SuccessMessage"] = "Recovery codes baru berhasil dibuat.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadRecoveryCodes()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        var codes = await _accountService.GetRecoveryCodesSnapshotAsync(
            user.Id,
            HttpContext.RequestAborted
        );

        if (codes == null || codes.Count == 0)
        {
            TempData["ErrorMessage"] = "Belum ada recovery code yang siap diunduh.";
            return RedirectToAction(nameof(Settings));
        }

        var builder = new StringBuilder();
        builder.AppendLine("Procurement HTE - Recovery Codes");
        builder.AppendLine("Simpan file ini di tempat aman. Jangan bagikan ke siapapun.");
        builder.AppendLine();
        foreach (var code in codes)
        {
            builder.AppendLine(code);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var fileName = $"recovery-codes-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
        return File(bytes, "text/plain", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideRecoveryCodes()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        await _accountService.SetRecoveryCodesHiddenAsync(
            user.Id,
            true,
            HttpContext.RequestAborted
        );

        TempData["SuccessMessage"] = "Recovery codes disembunyikan dari layar.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ShowRecoveryCodes()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        await _accountService.SetRecoveryCodesHiddenAsync(
            user.Id,
            false,
            HttpContext.RequestAborted
        );

        return RedirectToAction(nameof(Settings));
    }
}
