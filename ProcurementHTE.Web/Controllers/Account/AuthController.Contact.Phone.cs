using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendContactPhoneVerification(string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        if (_securityBypassOptions.BypassPhoneVerification || !IsSmsVerificationAvailable())
        {
            TempData["ErrorMessage"] = "Verifikasi nomor HP sementara dinonaktifkan.";
            return RedirectToAction(nameof(ContactVerification), new { returnUrl });
        }

        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            TempData["ErrorMessage"] = "Nomor HP belum diisi.";
            return RedirectToAction(nameof(ContactVerification), new { returnUrl });
        }

        if (IsCooldownActive(ContactPhoneCooldownKey, out var remaining))
        {
            TempData["ErrorMessage"] = $"Tunggu {remaining} detik sebelum mengirim ulang OTP.";
            return RedirectToAction(nameof(ContactVerification), new { returnUrl });
        }

        try
        {
            var code = await _accountService.GeneratePhoneVerificationCodeAsync(
                user.Id,
                HttpContext.RequestAborted
            );
            await _accountService.SendPhoneVerificationCodeAsync(
                user.Id,
                code,
                HttpContext.RequestAborted
            );
            TempData["SuccessMessage"] = "OTP verifikasi dikirim ke nomor Anda.";
            if (_smsOptions.UseDevelopmentMode)
                TempData["ContactDevPhoneOtp"] = code;
            StartCooldown(ContactPhoneCooldownKey);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal mengirim OTP verifikasi kontak: {ex.Message}";
        }

        return RedirectToAction(nameof(ContactVerification), new { returnUrl });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyContactPhone(string code, string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        if (_securityBypassOptions.BypassPhoneVerification || !IsSmsVerificationAvailable())
        {
            TempData["ErrorMessage"] = "Verifikasi nomor HP sementara dinonaktifkan.";
            return RedirectToAction(nameof(ContactVerification), new { returnUrl });
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["ErrorMessage"] = "Kode OTP wajib diisi.";
            return RedirectToAction(nameof(ContactVerification), new { returnUrl });
        }

        try
        {
            await _accountService.ConfirmPhoneAsync(user.Id, code, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Nomor HP berhasil diverifikasi.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal memverifikasi nomor HP: {ex.Message}";
        }

        return RedirectToAction(nameof(ContactVerification), new { returnUrl });
    }
}
