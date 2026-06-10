using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Web.Models.Account;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AccountController
{
    public async Task<IActionResult> Settings()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        var overview = await _accountService.GetOverviewAsync(user.Id, HttpContext.RequestAborted);
        var twoFactor = await _accountService.GetTwoFactorSummaryAsync(
            user.Id,
            HttpContext.RequestAborted
        );
        var sessions = await _accountService.GetSessionsAsync(user.Id, HttpContext.RequestAborted);
        sessions = sessions.Where(s => s.IsActive || s.IsCurrent).ToList();
        var logs = await _accountService.GetSecurityLogsAsync(
            user.Id,
            5,
            HttpContext.RequestAborted
        );
        var currentSessionId = GetCurrentSessionId();
        var recoveryCodes = overview.RecoveryCodesSnapshot?.ToArray();
        var profilePhoneInput = IndonesianPhoneNumberFormatter.FormatForInput(overview.PhoneNumber);

        var viewModel = new AccountSettingsViewModel
        {
            Overview = overview,
            Profile = new UpdateProfileInputModel
            {
                FirstName = overview.FirstName,
                LastName = overview.LastName,
                JobTitle = overview.JobTitle,
                Email = overview.Email,
                UserName = overview.UserName,
                PhoneNumber = profilePhoneInput,
            },
            ChangePassword = new ChangePasswordInputModel(),
            TwoFactor = new TwoFactorSettingsViewModel
            {
                IsEnabled = twoFactor.IsEnabled,
                SelectedMethod = twoFactor.Method,
                SharedKey = twoFactor.SharedKey,
                AuthenticatorUri = twoFactor.AuthenticatorUri,
                AuthenticatorQrBase64 = twoFactor.AuthenticatorQrBase64,
                RecoveryCodesLeft = twoFactor.RecoveryCodesLeft,
                NewlyGeneratedRecoveryCodes = recoveryCodes,
            },
            Sessions = sessions
                .Select(x => new UserSessionViewModel
                {
                    SessionId = x.UserSessionId,
                    Device = x.Device ?? "Tidak diketahui",
                    Browser = x.Browser ?? "Tidak diketahui",
                    IpAddress = x.IpAddress,
                    Location = x.Location,
                    CreatedAt = x.CreatedAt,
                    IsActive = x.IsActive,
                    IsCurrent = x.UserSessionId == currentSessionId,
                })
                .ToList(),
            SecurityLogs = logs,
        };

        ViewBag.ActivePage = "settings";
        ViewBag.ShowPhoneVerify = TempData["ShowPhoneVerify"]?.ToString() == "1";
        ViewBag.DevPhoneOtp = _smsOptions.UseDevelopmentMode ? TempData["DevPhoneOtp"] : null;
        ViewBag.DevMagicLink = _emailOptions.UseDevelopmentMode ? TempData["DevMagicLink"] : null;
        ViewBag.SmsVerificationAvailable =
            IsSmsVerificationAvailable() && !_securityBypassOptions.BypassPhoneVerification;
        ViewBag.RecoveryCodesHidden = overview.RecoveryCodesHidden;
        ViewBag.HasStoredRecoveryCodes = recoveryCodes?.Length > 0;
        ViewBag.EmailVerificationCooldown = GetCooldownSeconds(EmailVerificationCooldownKey);
        ViewBag.PhoneVerificationCooldown = GetCooldownSeconds(PhoneVerificationCooldownKey);
        ViewBag.RequirePhoneVerificationForTwoFactor =
            twoFactor.IsEnabled
            && twoFactor.Method == TwoFactorMethod.Sms
            && IsSmsVerificationAvailable()
            && !_securityBypassOptions.BypassPhoneVerification
            && !overview.PhoneNumberConfirmed;
        ViewBag.RequireEmailVerificationForTwoFactor =
            twoFactor.IsEnabled
            && twoFactor.Method == TwoFactorMethod.Email
            && !overview.EmailConfirmed;

        return View(viewModel);
    }
}
