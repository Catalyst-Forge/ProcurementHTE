using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    private IActionResult? RedirectIfContactVerificationRequired(User user, string? returnUrl)
    {
        if (_securityBypassOptions.BypassContactVerification)
            return null;

        if (!string.IsNullOrWhiteSpace(user.Email) && !user.EmailConfirmed)
            return RedirectToAction(nameof(ContactVerification), new { returnUrl });

        if (RequiresPhoneVerification(user))
            return RedirectToAction(nameof(ContactVerification), new { returnUrl });

        return null;
    }

    private IActionResult? RedirectIfTwoFactorSetupRequired(User user, string? returnUrl)
    {
        if (_securityBypassOptions.BypassTwoFactor)
            return null;

        if (!user.TwoFactorEnabled)
            return RedirectToAction(nameof(TwoFactorSetup), new { returnUrl });

        return null;
    }

    private bool RequiresPhoneVerification(User user) =>
        !_securityBypassOptions.BypassPhoneVerification
        && IsSmsVerificationAvailable()
        && !string.IsNullOrWhiteSpace(user.PhoneNumber)
        && !user.PhoneNumberConfirmed;

    private bool CanUseSmsTwoFactor(User user) =>
        !_securityBypassOptions.BypassPhoneVerification
        && IsSmsVerificationAvailable()
        && !string.IsNullOrWhiteSpace(user.PhoneNumber)
        && user.PhoneNumberConfirmed;

    private bool IsSmsVerificationAvailable() =>
        _smsOptions.UseDevelopmentMode
        || (
            !string.IsNullOrWhiteSpace(_smsOptions.ProviderUrl)
            && !string.IsNullOrWhiteSpace(_smsOptions.ApiKey)
        );

    private static string GetTabForMethod(TwoFactorMethod method) =>
        method switch
        {
            TwoFactorMethod.Email => "email",
            TwoFactorMethod.Sms => "sms",
            _ => "auth",
        };
}
