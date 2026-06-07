using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models.Auth;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmailResetCode(
        [Bind(Prefix = "EmailReset")] ForgotPasswordResetWithCodeViewModel model
    )
    {
        ModelState.Remove("EmailReset.Code");
        ModelState.Remove("EmailReset.NewPassword");
        ModelState.Remove("EmailReset.ConfirmPassword");
        var isAjax = IsAjaxRequest();
        if (!ModelState.IsValid)
            return isAjax ? AjaxModelError("Email tidak valid.") : ForgotPasswordView("email", emailReset: model);

        if (IsCooldownActive(ForgotEmailCooldownKey, out var remaining))
        {
            if (isAjax)
                return CooldownJsonResult(remaining);

            ModelState.AddModelError(
                string.Empty,
                $"Tunggu {remaining} detik sebelum mengirim ulang kode."
            );
            return ForgotPasswordView("email", emailReset: model);
        }

        string? devCode = null;
        var user = await FindUserAsync(model.Identifier);
        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
        {
            var code = await _accountService.GeneratePasswordResetEmailCodeAsync(
                user.Id,
                HttpContext.RequestAborted
            );
            await _accountService.SendPasswordResetEmailCodeAsync(
                user.Id,
                code,
                HttpContext.RequestAborted
            );
            if (_emailOptions.UseDevelopmentMode)
            {
                devCode = code;
                TempData["DevEmailResetCode"] = code;
            }
        }

        var message = "Jika akun ditemukan, kode reset dikirim ke email terdaftar.";
        TempData["SuccessMessage"] = message;
        StartCooldown(ForgotEmailCooldownKey);
        if (isAjax)
            return Ok(new { success = true, message, devCode, cooldown = CodeCooldownSeconds });

        return ForgotPasswordView("email", emailReset: model, emailCodeOverride: devCode);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordWithEmail(
        [Bind(Prefix = "EmailReset")] ForgotPasswordResetWithCodeViewModel model
    )
    {
        if (!ModelState.IsValid)
            return ForgotPasswordView("email", emailReset: model);

        var user = await FindUserAsync(model.Identifier);
        if (user == null)
        {
            ModelState.AddModelError("EmailReset.Identifier", "Akun tidak ditemukan.");
            return ForgotPasswordView("email", emailReset: model);
        }

        try
        {
            await _accountService.ResetPasswordWithEmailCodeAsync(
                user.Id,
                model.Code,
                model.NewPassword,
                HttpContext.RequestAborted
            );
            TempData["SuccessMessage"] = "Password berhasil direset. Silakan login.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                "EmailReset.Code",
                $"Gagal mereset password via email: {ex.Message}"
            );
            return ForgotPasswordView("email", emailReset: model);
        }
    }
}
