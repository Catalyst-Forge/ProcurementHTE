using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models.Auth;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    private void PopulateForgotPasswordDevHints(
        string? emailCodeOverride = null,
        string? smsCodeOverride = null
    )
    {
        if (_emailOptions.UseDevelopmentMode && !string.IsNullOrWhiteSpace(emailCodeOverride))
            TempData["DevEmailResetCode"] = emailCodeOverride;

        if (_smsOptions.UseDevelopmentMode && !string.IsNullOrWhiteSpace(smsCodeOverride))
            TempData["DevSmsResetCode"] = smsCodeOverride;

        ViewBag.DevEmailResetCode = _emailOptions.UseDevelopmentMode
            ? emailCodeOverride ?? TempData.Peek("DevEmailResetCode")?.ToString()
            : null;

        ViewBag.DevSmsResetCode = _smsOptions.UseDevelopmentMode
            ? smsCodeOverride ?? TempData.Peek("DevSmsResetCode")?.ToString()
            : null;
    }

    private void PopulateForgotPasswordCooldowns()
    {
        ViewBag.ForgotEmailCooldown = GetCooldownSeconds(ForgotEmailCooldownKey);
        ViewBag.ForgotSmsCooldown = GetCooldownSeconds(ForgotSmsCooldownKey);
    }

    private IActionResult ForgotPasswordView(
        string activeTab,
        ForgotPasswordRecoveryViewModel? recovery = null,
        ForgotPasswordResetWithCodeViewModel? emailReset = null,
        ForgotPasswordResetWithCodeViewModel? smsReset = null,
        string? emailCodeOverride = null,
        string? smsCodeOverride = null
    )
    {
        PopulateForgotPasswordDevHints(emailCodeOverride, smsCodeOverride);
        PopulateForgotPasswordCooldowns();
        return View(
            "ForgotPassword",
            BuildForgotPasswordPage(activeTab, recovery, emailReset, smsReset)
        );
    }

    private static ForgotPasswordPageViewModel BuildForgotPasswordPage(
        string activeTab = "recovery",
        ForgotPasswordRecoveryViewModel? recovery = null,
        ForgotPasswordResetWithCodeViewModel? emailReset = null,
        ForgotPasswordResetWithCodeViewModel? smsReset = null
    )
    {
        return new ForgotPasswordPageViewModel
        {
            ActiveTab = string.IsNullOrWhiteSpace(activeTab) ? "recovery" : activeTab,
            Recovery = recovery ?? new ForgotPasswordRecoveryViewModel(),
            EmailReset = emailReset ?? new ForgotPasswordResetWithCodeViewModel(),
            SmsReset = smsReset ?? new ForgotPasswordResetWithCodeViewModel(),
        };
    }

    private static string NormalizeRecoveryCode(string code) =>
        (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);

    private bool NeedsRecoveryReset() =>
        HttpContext.Session.GetString(RecoveryResetSessionKey) == "1";
}
