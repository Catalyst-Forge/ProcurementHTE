using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models.Auth;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendSmsResetCode(
        [Bind(Prefix = "SmsReset")] ForgotPasswordResetWithCodeViewModel model
    )
    {
        ModelState.Remove("SmsReset.Code");
        ModelState.Remove("SmsReset.NewPassword");
        ModelState.Remove("SmsReset.ConfirmPassword");
        var isAjax = IsAjaxRequest();
        if (!ModelState.IsValid)
            return isAjax ? AjaxModelError("Nomor HP tidak valid.") : ForgotPasswordView("sms", smsReset: model);

        if (IsCooldownActive(ForgotSmsCooldownKey, out var smsRemaining))
        {
            if (isAjax)
                return CooldownJsonResult(smsRemaining);

            ModelState.AddModelError(
                string.Empty,
                $"Tunggu {smsRemaining} detik sebelum mengirim ulang kode."
            );
            return ForgotPasswordView("sms", smsReset: model);
        }

        string? devCode = null;
        var user = await FindUserByPhoneAsync(model.Identifier);
        if (user == null)
        {
            var notFoundMessage = "Nomor HP tidak ditemukan.";
            TempData["ErrorMessage"] = notFoundMessage;
            if (isAjax)
                return BadRequest(new { success = false, message = notFoundMessage });

            return ForgotPasswordView("sms", smsReset: model);
        }

        var code = await _accountService.GeneratePasswordResetSmsCodeAsync(
            user.Id,
            HttpContext.RequestAborted
        );
        await _accountService.SendPasswordResetSmsCodeAsync(
            user.Id,
            code,
            HttpContext.RequestAborted
        );

        if (_smsOptions.UseDevelopmentMode)
        {
            devCode = code;
            TempData["DevSmsResetCode"] = code;
        }

        var message = "Kode reset dikirim via SMS.";
        TempData["SuccessMessage"] = message;
        StartCooldown(ForgotSmsCooldownKey);
        if (isAjax)
            return Ok(new { success = true, message, devCode, cooldown = CodeCooldownSeconds });

        return ForgotPasswordView("sms", smsReset: model, smsCodeOverride: devCode);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordWithSms(
        [Bind(Prefix = "SmsReset")] ForgotPasswordResetWithCodeViewModel model
    )
    {
        if (!ModelState.IsValid)
            return ForgotPasswordView("sms", smsReset: model);

        var user = await FindUserByPhoneAsync(model.Identifier);
        if (user == null)
        {
            ModelState.AddModelError("SmsReset.Identifier", "Nomor HP tidak ditemukan.");
            return ForgotPasswordView("sms", smsReset: model);
        }

        try
        {
            await _accountService.ResetPasswordWithSmsCodeAsync(
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
                "SmsReset.Code",
                $"Gagal mereset password via SMS: {ex.Message}"
            );
            return ForgotPasswordView("sms", smsReset: model);
        }
    }
}
