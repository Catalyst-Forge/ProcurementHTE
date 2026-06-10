using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTwoFactorSetupCode(TwoFactorMethod method)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "Sesi pengguna tidak ditemukan." });

        var validation = ValidateTwoFactorCodeRequest(user, method);
        if (validation != null)
            return validation;

        var cooldownKey = method switch
        {
            TwoFactorMethod.Email => TwoFactorEmailCooldownKey,
            TwoFactorMethod.Sms => TwoFactorSmsCooldownKey,
            _ => null,
        };

        if (cooldownKey != null && IsCooldownActive(cooldownKey, out var remaining))
            return CooldownJsonResult(remaining);

        try
        {
            var code = await _accountService.GenerateTwoFactorCodeAsync(
                user.Id,
                method,
                HttpContext.RequestAborted
            );
            await _accountService.SendTwoFactorCodeAsync(
                user.Id,
                method,
                code,
                HttpContext.RequestAborted
            );
            if (cooldownKey != null)
                StartCooldown(cooldownKey);

            return Json(
                new
                {
                    success = true,
                    message = method == TwoFactorMethod.Email
                        ? "Kode dikirim ke email Anda."
                        : "Kode dikirim via SMS.",
                    devCode = GetDevelopmentTwoFactorCode(method, code),
                    cooldown = CodeCooldownSeconds,
                }
            );
        }
        catch (Exception ex)
        {
            return Json(
                new
                {
                    success = false,
                    message = $"Gagal mengirim kode verifikasi: {ex.Message}",
                }
            );
        }
    }

    private JsonResult? ValidateTwoFactorCodeRequest(
        ProcurementHTE.Core.Models.User user,
        TwoFactorMethod method
    )
    {
        if (method == TwoFactorMethod.AuthenticatorApp)
        {
            return Json(
                new
                {
                    success = false,
                    message = "Authenticator tidak membutuhkan kode tambahan.",
                }
            );
        }

        if (method == TwoFactorMethod.Email && !user.EmailConfirmed)
            return Json(new { success = false, message = "Email belum terverifikasi." });

        if (method == TwoFactorMethod.Sms && RequiresPhoneVerification(user))
            return Json(new { success = false, message = "Nomor HP belum terverifikasi." });

        if (method == TwoFactorMethod.Sms && !CanUseSmsTwoFactor(user))
        {
            return Json(
                new
                {
                    success = false,
                    message = IsSmsVerificationAvailable()
                        ? "Nomor HP belum siap untuk SMS OTP."
                        : "SMS OTP belum dikonfigurasi.",
                }
            );
        }

        return null;
    }

    private string? GetDevelopmentTwoFactorCode(TwoFactorMethod method, string code)
    {
        return method == TwoFactorMethod.Email && _emailOptions.UseDevelopmentMode ? code
            : method == TwoFactorMethod.Sms && _smsOptions.UseDevelopmentMode ? code
            : null;
    }
}
