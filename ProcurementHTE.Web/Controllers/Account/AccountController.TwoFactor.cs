using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AccountController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactor(
        TwoFactorMethod method,
        string verificationCode
    )
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        if (method == TwoFactorMethod.Sms && !CanUseSmsTwoFactor(user))
        {
            TempData["ErrorMessage"] = IsSmsVerificationAvailable()
                ? "Nomor HP belum siap untuk SMS OTP."
                : "SMS OTP belum dikonfigurasi.";
            return RedirectToAction(nameof(Settings));
        }

        try
        {
            await _accountService.EnableTwoFactorAsync(
                user.Id,
                method,
                verificationCode,
                HttpContext.RequestAborted
            );
            TempData["SuccessMessage"] = "Two-factor authentication berhasil diaktifkan.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] =
                $"Gagal mengaktifkan two-factor authentication: {ex.Message}";
        }

        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        try
        {
            await _accountService.DisableTwoFactorAsync(user.Id, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Two-factor authentication dinonaktifkan.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] =
                $"Gagal menonaktifkan two-factor authentication: {ex.Message}";
        }

        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTwoFactorCode(TwoFactorMethod method)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Json(new { success = false, message = "Sesi user tidak ditemukan." });
        }

        if (method == TwoFactorMethod.AuthenticatorApp)
        {
            return Json(
                new
                {
                    success = false,
                    message = "Authenticator tidak membutuhkan kode pengiriman.",
                }
            );
        }

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

            var devCode = method switch
            {
                TwoFactorMethod.Email when _emailOptions.UseDevelopmentMode => code,
                TwoFactorMethod.Sms when _smsOptions.UseDevelopmentMode => code,
                _ => null,
            };
            var channel = method == TwoFactorMethod.Email ? "email" : "SMS";
            var message = $"Kode verifikasi dikirim melalui {channel}.";

            return Json(
                new
                {
                    success = true,
                    message,
                    devCode,
                }
            );
        }
        catch (Exception ex)
        {
            return Json(
                new { success = false, message = $"Gagal mengirim kode verifikasi: {ex.Message}" }
            );
        }
    }
}
