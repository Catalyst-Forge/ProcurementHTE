using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models.Auth;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        PopulateForgotPasswordCooldowns();
        return ForgotPasswordView("recovery");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordWithRecovery(
        [Bind(Prefix = "Recovery")] ForgotPasswordRecoveryViewModel model
    )
    {
        if (!ModelState.IsValid)
            return ForgotPasswordView("recovery", recovery: model);

        var user = await FindUserAsync(model.Identifier);
        if (user == null)
        {
            ModelState.AddModelError("Recovery.Identifier", "Akun tidak ditemukan.");
            return ForgotPasswordView("recovery", recovery: model);
        }

        try
        {
            await _accountService.ResetPasswordWithRecoveryCodeAsync(
                user.Id,
                model.RecoveryCode,
                model.NewPassword,
                HttpContext.RequestAborted
            );
            TempData["SuccessMessage"] = "Password berhasil direset. Silakan login.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                "Recovery.RecoveryCode",
                $"Gagal mereset password dengan recovery code: {ex.Message}"
            );
            return ForgotPasswordView("recovery", recovery: model);
        }
    }
}
