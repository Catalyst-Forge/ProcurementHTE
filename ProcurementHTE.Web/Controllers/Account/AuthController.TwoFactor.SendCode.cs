using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTwoFactorCode()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            return Json(new { success = false, message = "Sesi 2FA tidak ditemukan." });

        if (user.TwoFactorMethod == TwoFactorMethod.AuthenticatorApp)
        {
            return Json(
                new
                {
                    success = false,
                    message = "Authenticator tidak membutuhkan kode tambahan.",
                }
            );
        }

        var code = await _accountService.GenerateTwoFactorCodeAsync(
            user.Id,
            user.TwoFactorMethod,
            HttpContext.RequestAborted
        );

        await _accountService.SendTwoFactorCodeAsync(
            user.Id,
            user.TwoFactorMethod,
            code,
            HttpContext.RequestAborted
        );

        var channel = user.TwoFactorMethod == TwoFactorMethod.Email ? "email" : "SMS";
        return Json(
            new
            {
                success = true,
                message = $"Kode verifikasi dikirim melalui {channel}.",
                devCode = GetDevelopmentTwoFactorCode(user.TwoFactorMethod, code),
            }
        );
    }
}
