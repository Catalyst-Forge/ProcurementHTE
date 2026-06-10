using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Options;
using ProcurementHTE.Web.Hubs;

namespace ProcurementHTE.Web.Controllers.Account
{
    public partial class AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAccountService accountService,
        IOptions<EmailSenderOptions> emailOptions,
        IOptions<SmsSenderOptions> smsOptions,
        IOptions<SecurityBypassOptions> bypassOptions,
        IUserActivityNotifier userActivityNotifier
    ) : Controller
    {
        private const string RecoveryResetSessionKey = "Auth.RequireRecoveryReset";
        private const int CodeCooldownSeconds = 60;
        private const string ForgotEmailCooldownKey = "forgot.email";
        private const string ForgotSmsCooldownKey = "forgot.sms";
        private const string TwoFactorEmailCooldownKey = "setup.email";
        private const string TwoFactorSmsCooldownKey = "setup.sms";
        private const string ContactEmailCooldownKey = "contact.email";
        private const string ContactPhoneCooldownKey = "contact.phone";
        private const string PendingEmailCooldownKey = "pending.email";
        private const string PendingPhoneCooldownKey = "pending.phone";
        private const string TwoFactorLoginEmailCooldownKey = "login.email";
        private const string TwoFactorLoginSmsCooldownKey = "login.sms";

        private readonly UserManager<User> _userManager = userManager;
        private readonly SignInManager<User> _signInManager = signInManager;
        private readonly IAccountService _accountService = accountService;
        private readonly EmailSenderOptions _emailOptions = emailOptions.Value;
        private readonly SmsSenderOptions _smsOptions = smsOptions.Value;
        private readonly SecurityBypassOptions _securityBypassOptions =
            bypassOptions.Value ?? new SecurityBypassOptions();
        private readonly IUserActivityNotifier _userActivityNotifier = userActivityNotifier;
    }
}
