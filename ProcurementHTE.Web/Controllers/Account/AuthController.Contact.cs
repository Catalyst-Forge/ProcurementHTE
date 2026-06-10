using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models.Auth;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [Authorize]
    public async Task<IActionResult> ContactVerification(string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        var requireEmail = !string.IsNullOrWhiteSpace(user.Email) && !user.EmailConfirmed;
        var requirePhone = RequiresPhoneVerification(user);

        if (!requireEmail && !requirePhone)
        {
            var setupRedirect = RedirectIfTwoFactorSetupRequired(user, returnUrl);
            return setupRedirect ?? RedirectToLocal(returnUrl);
        }

        var model = new ContactVerificationViewModel
        {
            Email = user.Email ?? "-",
            PhoneNumber = user.PhoneNumber,
            RequiresEmail = requireEmail,
            RequiresPhone = requirePhone,
            ReturnUrl = returnUrl,
            DevMagicLink = _emailOptions.UseDevelopmentMode
                ? TempData["ContactDevMagicLink"] as string
                : null,
            DevPhoneOtp = _smsOptions.UseDevelopmentMode
                ? TempData["ContactDevPhoneOtp"] as string
                : null,
        };

        ViewBag.ContactEmailCooldown = GetCooldownSeconds(ContactEmailCooldownKey);
        ViewBag.ContactPhoneCooldown = GetCooldownSeconds(ContactPhoneCooldownKey);
        ViewBag.PendingEmailCooldown = GetCooldownSeconds(PendingEmailCooldownKey);
        ViewBag.PendingPhoneCooldown = GetCooldownSeconds(PendingPhoneCooldownKey);

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendContactEmailVerification(string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login");

        if (IsCooldownActive(ContactEmailCooldownKey, out var remaining))
        {
            TempData["ErrorMessage"] =
                $"Tunggu {remaining} detik sebelum mengirim ulang magic link.";
            return RedirectToAction(nameof(ContactVerification), new { returnUrl });
        }

        try
        {
            var callbackUrl = await BuildEmailVerificationCallbackUrlAsync(user.Id);
            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                TempData["ErrorMessage"] = "Tidak dapat membuat magic link saat ini.";
            }
            else
            {
                await _accountService.SendEmailVerificationAsync(
                    user.Id,
                    callbackUrl,
                    HttpContext.RequestAborted
                );
                TempData["SuccessMessage"] = "Magic link telah dikirim ke email Anda.";
                if (_emailOptions.UseDevelopmentMode)
                    TempData["ContactDevMagicLink"] = callbackUrl;
                StartCooldown(ContactEmailCooldownKey);
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal mengirim magic link verifikasi email: {ex.Message}";
        }

        return RedirectToAction(nameof(ContactVerification), new { returnUrl });
    }
}
