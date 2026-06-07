using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models.Auth;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [Authorize]
    public IActionResult RecoveryReset()
    {
        if (!NeedsRecoveryReset())
            return Redirect("~/");

        return View(new RecoveryResetViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecoveryReset(RecoveryResetViewModel model)
    {
        if (!NeedsRecoveryReset())
            return Redirect("~/");

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Sesi berakhir. Silakan login ulang.";
            return RedirectToAction(nameof(Login));
        }

        try
        {
            await _accountService.ResetPasswordForUserAsync(
                user.Id,
                model.NewPassword,
                "Password diperbarui setelah login dengan recovery code.",
                HttpContext.RequestAborted
            );
            HttpContext.Session.Remove(RecoveryResetSessionKey);
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Password berhasil diperbarui.";
            return Redirect("~/");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                $"Gagal memperbarui password dengan recovery reset: {ex.Message}"
            );
            return View(model);
        }
    }
}
