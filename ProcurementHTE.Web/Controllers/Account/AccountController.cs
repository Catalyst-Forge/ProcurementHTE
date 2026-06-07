using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Options;

namespace ProcurementHTE.Web.Controllers.Account;

[Authorize]
public partial class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IAccountService _accountService;
    private readonly EmailSenderOptions _emailOptions;
    private readonly SmsSenderOptions _smsOptions;
    private readonly SecurityBypassOptions _securityBypassOptions;
    private const int VerificationCooldownSeconds = 60;
    private const string EmailVerificationCooldownKey = "settings.email";
    private const string PhoneVerificationCooldownKey = "settings.phone";

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAccountService accountService,
        IOptions<EmailSenderOptions> emailOptions,
        IOptions<SmsSenderOptions> smsOptions,
        IOptions<SecurityBypassOptions> bypassOptions
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _accountService = accountService;
        _emailOptions = emailOptions.Value;
        _smsOptions = smsOptions.Value;
        _securityBypassOptions = bypassOptions.Value ?? new SecurityBypassOptions();
    }
}
