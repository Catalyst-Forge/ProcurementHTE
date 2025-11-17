using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.Enums;
using ProcurementHTE.Core.Models.ViewModels;
using ProcurementHTE.Core.Options;
using ProcurementHTE.Web.Constants;
using ProcurementHTE.Web.Helpers;
using ProcurementHTE.Web.Models.Auth;
using ProcurementHTE.Web.Options;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ProcurementHTE.Web.Controllers
{
    public class AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IAccountService accountService,
        IOptions<EmailSenderOptions> emailOptions,
        IOptions<SmsSenderOptions> smsOptions,
        IOptions<SecurityBypassOptions> bypassOptions,
        ILogger<AuthController> logger
    ) : Controller
    {
        private const string RecoveryResetSessionKey = "Auth.RequireRecoveryReset";

        private readonly UserManager<User> _userManager = userManager;
        private readonly SignInManager<User> _signInManager = signInManager;
        private readonly IAccountService _accountService = accountService;
        private readonly EmailSenderOptions _emailOptions = emailOptions.Value;
        private readonly SmsSenderOptions _smsOptions = smsOptions.Value;
        private readonly SecurityBypassOptions _securityBypassOptions = bypassOptions.Value ?? new SecurityBypassOptions();
        private readonly ILogger<AuthController> _logger = logger;

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

        /*
         * GET: Login
         */
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /*
         * POST: Login
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await FindUserAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Akun tidak ditemukan atau tidak aktif");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                await CompleteInteractiveLoginAsync(user, model.RememberMe, returnUrl);
                var checkpointRedirect = RedirectIfContactVerificationRequired(user, returnUrl);
                if (checkpointRedirect != null)
                    return checkpointRedirect;
                var setupRedirect = RedirectIfTwoFactorSetupRequired(user, returnUrl);
                if (setupRedirect != null)
                    return setupRedirect;
                return RedirectToLocal(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                if (_securityBypassOptions.BypassTwoFactor)
                {
                    await _signInManager.SignInAsync(user, model.RememberMe);
                    await CompleteInteractiveLoginAsync(user, model.RememberMe, returnUrl);
                    var checkpointRedirect = RedirectIfContactVerificationRequired(user, returnUrl);
                    if (checkpointRedirect != null)
                        return checkpointRedirect;
                    return RedirectToLocal(returnUrl);
                }

                return RedirectToAction(
                    nameof(LoginWith2fa),
                    new { returnUrl, rememberMe = model.RememberMe }
                );
            }

            if (result.IsLockedOut)
            {
                await _accountService.LogEventAsync(
                    user.Id,
                    SecurityLogEventType.LoginFailed,
                    false,
                    "Akun terkunci saat login.",
                    GetRemoteIp(),
                    GetUserAgent(),
                    HttpContext.RequestAborted
                );
                ModelState.AddModelError(string.Empty, "Akun Anda terkunci. Coba lagi nanti.");
            }
            else
            {
                await _accountService.LogEventAsync(
                    user.Id,
                    SecurityLogEventType.LoginFailed,
                    false,
                    "Email atau password salah.",
                    GetRemoteIp(),
                    GetUserAgent(),
                    HttpContext.RequestAborted
                );
                ModelState.AddModelError(string.Empty, "Email atau Password salah");
            }

            return View(model);
        }

        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Tidak dapat memuat pengguna untuk 2FA.");
            }

            var viewModel = new LoginWith2faViewModel
            {
                RememberMe = rememberMe,
                Method = user.TwoFactorMethod,
                ReturnUrl = returnUrl,
            };

            ViewBag.EmailAddress = user.Email;
            ViewBag.PhoneNumber = user.PhoneNumber;
            var recoveryCodes = await _accountService.GetRecoveryCodesSnapshotAsync(
                user.Id,
                HttpContext.RequestAborted
            );
            ViewBag.HasRecoveryCodes = recoveryCodes?.Any() == true;

            var requiresEmailVerification =
                user.TwoFactorMethod == TwoFactorMethod.Email && !user.EmailConfirmed;
            var requiresPhoneVerification =
                user.TwoFactorMethod == TwoFactorMethod.Sms && !user.PhoneNumberConfirmed;

            if (requiresEmailVerification)
            {
                ViewBag.RequireEmailVerification = true;
                try
                {
                    var encodedToken = await _accountService.GenerateEmailVerificationTokenAsync(
                        user.Id,
                        HttpContext.RequestAborted
                    );
                    var callbackUrl = Url.Action(
                        "ConfirmEmail",
                        "Account",
                        new { userId = user.Id, token = encodedToken },
                        Request.Scheme
                    );

                    if (string.IsNullOrWhiteSpace(callbackUrl))
                    {
                        ViewBag.Auto2faError = "Tidak dapat membuat tautan verifikasi email saat ini.";
                    }
                    else
                    {
                        await _accountService.SendEmailVerificationAsync(
                            user.Id,
                            callbackUrl,
                            HttpContext.RequestAborted
                        );
                        if (_emailOptions.UseDevelopmentMode)
                        {
                            ViewBag.DevMagicLink = callbackUrl;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gagal mengirim magic link otomatis untuk user {User}", user.Id);
                    ViewBag.Auto2faError = ex.Message;
                }
            }
            else if (requiresPhoneVerification)
            {
                ViewBag.RequirePhoneVerification = true;
                try
                {
                    var verificationCode = await _accountService.GeneratePhoneVerificationCodeAsync(
                        user.Id,
                        HttpContext.RequestAborted
                    );
                    await _accountService.SendPhoneVerificationCodeAsync(
                        user.Id,
                        verificationCode,
                        HttpContext.RequestAborted
                    );
                    if (_smsOptions.UseDevelopmentMode)
                    {
                        ViewBag.DevPhoneOtp = verificationCode;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Gagal mengirim OTP verifikasi nomor untuk user {User}",
                        user.Id
                    );
                    ViewBag.Auto2faError = ex.Message;
                }
            }
            else if (user.TwoFactorMethod is TwoFactorMethod.Email or TwoFactorMethod.Sms)
            {
                try
                {
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
                    ViewBag.Auto2faMessage =
                        user.TwoFactorMethod == TwoFactorMethod.Email
                            ? "Kode verifikasi dikirim ke email terdaftar Anda."
                            : "Kode verifikasi dikirim via SMS.";

                    var devCode =
                        user.TwoFactorMethod == TwoFactorMethod.Email && _emailOptions.UseDevelopmentMode
                            ? code
                            : user.TwoFactorMethod == TwoFactorMethod.Sms && _smsOptions.UseDevelopmentMode
                                ? code
                                : null;
                    if (devCode != null)
                    {
                        ViewBag.DevTwoFactorCode = devCode;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gagal mengirim kode 2FA otomatis untuk user {User}", user.Id);
                    ViewBag.Auto2faError = ex.Message;
                }

                var loginCooldownKey =
                    user.TwoFactorMethod == TwoFactorMethod.Email
                        ? TwoFactorLoginEmailCooldownKey
                        : user.TwoFactorMethod == TwoFactorMethod.Sms
                            ? TwoFactorLoginSmsCooldownKey
                            : null;

                if (loginCooldownKey != null)
                {
                    StartCooldown(loginCooldownKey);
                }
            }

            ViewBag.PendingEmailCooldown = GetCooldownSeconds(PendingEmailCooldownKey);
            ViewBag.PendingPhoneCooldown = GetCooldownSeconds(PendingPhoneCooldownKey);
            ViewBag.LoginEmailCooldown = GetCooldownSeconds(TwoFactorLoginEmailCooldownKey);
            ViewBag.LoginSmsCooldown = GetCooldownSeconds(TwoFactorLoginSmsCooldownKey);

            ViewData["ReturnUrl"] = returnUrl;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendPendingEmailVerification(bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");

            if (user.EmailConfirmed)
            {
                TempData["TwoFactorFlashSuccess"] = "Email sudah terverifikasi.";
                return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
            }

            if (IsCooldownActive(PendingEmailCooldownKey, out var remaining))
            {
                TempData["TwoFactorFlashError"] = $"Tunggu {remaining} detik sebelum mengirim ulang magic link.";
                return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
            }

            try
            {
                var encodedToken = await _accountService.GenerateEmailVerificationTokenAsync(
                    user.Id,
                    HttpContext.RequestAborted
                );
                var callbackUrl = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token = encodedToken },
                    Request.Scheme
                );
                if (string.IsNullOrWhiteSpace(callbackUrl))
                {
                    TempData["TwoFactorFlashError"] = "Tidak dapat membuat magic link saat ini.";
                }
                else
                {
                    await _accountService.SendEmailVerificationAsync(
                        user.Id,
                        callbackUrl,
                        HttpContext.RequestAborted
                    );
                    TempData["TwoFactorFlashSuccess"] = "Magic link verifikasi telah dikirim ulang.";
                    if (_emailOptions.UseDevelopmentMode)
                        TempData["TwoFactorFlashDevLink"] = callbackUrl;
                    StartCooldown(PendingEmailCooldownKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mengirim ulang magic link untuk user {User}", user.Id);
                TempData["TwoFactorFlashError"] = ex.Message;
            }

            return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendPendingPhoneVerification(bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");

            if (user.PhoneNumberConfirmed)
            {
                TempData["TwoFactorFlashSuccess"] = "Nomor HP sudah terverifikasi.";
                return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
            }

            if (IsCooldownActive(PendingPhoneCooldownKey, out var remaining))
            {
                TempData["TwoFactorFlashError"] = $"Tunggu {remaining} detik sebelum mengirim ulang OTP.";
                return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
            }

            try
            {
                var verificationCode = await _accountService.GeneratePhoneVerificationCodeAsync(
                    user.Id,
                    HttpContext.RequestAborted
                );
                await _accountService.SendPhoneVerificationCodeAsync(
                    user.Id,
                    verificationCode,
                    HttpContext.RequestAborted
                );
                TempData["TwoFactorFlashSuccess"] = "OTP verifikasi SMS telah dikirim ulang.";
                if (_smsOptions.UseDevelopmentMode)
                    TempData["TwoFactorFlashDevOtp"] = verificationCode;
                StartCooldown(PendingPhoneCooldownKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mengirim ulang OTP verifikasi HP untuk user {User}", user.Id);
                TempData["TwoFactorFlashError"] = ex.Message;
            }

            return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPendingPhone(string code, bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["TwoFactorFlashError"] = "Kode OTP harus diisi.";
                return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
            }

            try
            {
                await _accountService.ConfirmPhoneAsync(
                    user.Id,
                    code,
                    HttpContext.RequestAborted
                );
                TempData["TwoFactorFlashSuccess"] = "Nomor HP berhasil diverifikasi. Silakan lanjutkan login.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Verifikasi nomor HP 2FA gagal untuk user {User}", user.Id);
                TempData["TwoFactorFlashError"] = ex.Message;
            }

            return RedirectToAction(nameof(LoginWith2fa), new { rememberMe, returnUrl });
        }

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
                SuccessMessage = TempData["ContactSuccess"] as string,
                ErrorMessage = TempData["ContactError"] as string,
                DevMagicLink = _emailOptions.UseDevelopmentMode ? TempData["ContactDevMagicLink"] as string : null,
                DevPhoneOtp = _smsOptions.UseDevelopmentMode ? TempData["ContactDevPhoneOtp"] as string : null,
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
                TempData["ContactError"] = $"Tunggu {remaining} detik sebelum mengirim ulang magic link.";
                return RedirectToAction(nameof(ContactVerification), new { returnUrl });
            }

            try
            {
                var encodedToken = await _accountService.GenerateEmailVerificationTokenAsync(
                    user.Id,
                    HttpContext.RequestAborted
                );
                var callbackUrl = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token = encodedToken },
                    Request.Scheme
                );

                if (string.IsNullOrWhiteSpace(callbackUrl))
                {
                    TempData["ContactError"] = "Tidak dapat membuat magic link saat ini.";
                }
                else
                {
                    await _accountService.SendEmailVerificationAsync(
                        user.Id,
                        callbackUrl,
                        HttpContext.RequestAborted
                    );
                    TempData["ContactSuccess"] = "Magic link telah dikirim ke email Anda.";
                    if (_emailOptions.UseDevelopmentMode)
                        TempData["ContactDevMagicLink"] = callbackUrl;
                    StartCooldown(ContactEmailCooldownKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mengirim magic link verifikasi untuk user {UserId}", user?.Id);
                TempData["ContactError"] = ex.Message;
            }

            return RedirectToAction(nameof(ContactVerification), new { returnUrl });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContactPhoneVerification(string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                TempData["ContactError"] = "Nomor HP belum diisi.";
                return RedirectToAction(nameof(ContactVerification), new { returnUrl });
            }

            if (IsCooldownActive(ContactPhoneCooldownKey, out var remaining))
            {
                TempData["ContactError"] = $"Tunggu {remaining} detik sebelum mengirim ulang OTP.";
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
                TempData["ContactSuccess"] = "OTP verifikasi dikirim ke nomor Anda.";
                if (_smsOptions.UseDevelopmentMode)
                    TempData["ContactDevPhoneOtp"] = code;
                StartCooldown(ContactPhoneCooldownKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mengirim OTP verifikasi nomor untuk user {UserId}", user.Id);
                TempData["ContactError"] = ex.Message;
            }

            return RedirectToAction(nameof(ContactVerification), new { returnUrl });
        }

        [Authorize]
        public async Task<IActionResult> TwoFactorSetup(string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (user.TwoFactorEnabled)
                return RedirectToLocal(returnUrl);

            var summary = await _accountService.GetTwoFactorSummaryAsync(user.Id, HttpContext.RequestAborted);
            var emailAvailable = !string.IsNullOrWhiteSpace(user.Email) && user.EmailConfirmed;
            var phoneAvailable = RequiresPhoneVerification(user) == false && !string.IsNullOrWhiteSpace(user.PhoneNumber);
            var requestedTab = TempData["TwoFactorSetupActiveTab"] as string;
            var defaultTab = string.IsNullOrWhiteSpace(requestedTab) ? "auth" : requestedTab.ToLowerInvariant();
            if (defaultTab == "email" && !emailAvailable)
                defaultTab = phoneAvailable ? "sms" : "auth";
            if (defaultTab == "sms" && !phoneAvailable)
                defaultTab = emailAvailable ? "email" : "auth";

            var model = new TwoFactorSetupViewModel
            {
                EmailAvailable = emailAvailable,
                PhoneAvailable = phoneAvailable,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                SharedKey = summary.SharedKey,
                AuthenticatorUri = summary.AuthenticatorUri,
                AuthenticatorQrBase64 = summary.AuthenticatorQrBase64,
                ReturnUrl = returnUrl,
                SuccessMessage = TempData["TwoFactorSetupSuccess"] as string,
                ErrorMessage = TempData["TwoFactorSetupError"] as string,
                ActiveTab = defaultTab,
            };

            ViewBag.SetupEmailCooldown = GetCooldownSeconds(TwoFactorEmailCooldownKey);
            ViewBag.SetupSmsCooldown = GetCooldownSeconds(TwoFactorSmsCooldownKey);

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactorFromSetup(
            TwoFactorMethod method,
            string verificationCode,
            string? returnUrl = null
        )
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(verificationCode))
            {
                TempData["TwoFactorSetupError"] = "Kode verifikasi wajib diisi.";
                TempData["TwoFactorSetupActiveTab"] = GetTabForMethod(method);
                return RedirectToAction(nameof(TwoFactorSetup), new { returnUrl });
            }

            if (method == TwoFactorMethod.Email && !user.EmailConfirmed)
            {
                TempData["TwoFactorSetupError"] = "Email belum terverifikasi.";
                TempData["TwoFactorSetupActiveTab"] = GetTabForMethod(method);
                return RedirectToAction(nameof(TwoFactorSetup), new { returnUrl });
            }

            if (method == TwoFactorMethod.Sms && RequiresPhoneVerification(user))
            {
                TempData["TwoFactorSetupError"] = "Nomor HP belum terverifikasi.";
                TempData["TwoFactorSetupActiveTab"] = GetTabForMethod(method);
                return RedirectToAction(nameof(TwoFactorSetup), new { returnUrl });
            }

            try
            {
                await _accountService.EnableTwoFactorAsync(
                    user.Id,
                    method,
                    verificationCode,
                    HttpContext.RequestAborted
                );
                TempData["TwoFactorSetupSuccess"] = "Two-factor authentication berhasil diaktifkan.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mengaktifkan 2FA untuk user {UserId}", user.Id);
                TempData["TwoFactorSetupError"] = ex.Message;
                TempData["TwoFactorSetupActiveTab"] = GetTabForMethod(method);
                return RedirectToAction(nameof(TwoFactorSetup), new { returnUrl });
            }

            return RedirectToLocal(returnUrl);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTwoFactorSetupCode(TwoFactorMethod method)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "Sesi pengguna tidak ditemukan." });

            if (method == TwoFactorMethod.AuthenticatorApp)
            {
                return Json(new { success = false, message = "Authenticator tidak membutuhkan kode tambahan." });
            }

            if (method == TwoFactorMethod.Email && !user.EmailConfirmed)
            {
                return Json(new { success = false, message = "Email belum terverifikasi." });
            }

            if (method == TwoFactorMethod.Sms && RequiresPhoneVerification(user))
            {
                return Json(new { success = false, message = "Nomor HP belum terverifikasi." });
            }

            var cooldownKey = method switch
            {
                TwoFactorMethod.Email => TwoFactorEmailCooldownKey,
                TwoFactorMethod.Sms => TwoFactorSmsCooldownKey,
                _ => null,
            };

            if (cooldownKey != null && IsCooldownActive(cooldownKey, out var remaining))
            {
                return CooldownJsonResult(remaining);
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
                var message = method == TwoFactorMethod.Email
                    ? "Kode dikirim ke email Anda."
                    : "Kode dikirim via SMS.";
                var devCode =
                    method == TwoFactorMethod.Email && _emailOptions.UseDevelopmentMode
                        ? code
                        : method == TwoFactorMethod.Sms && _smsOptions.UseDevelopmentMode
                            ? code
                            : null;
                if (cooldownKey != null)
                    StartCooldown(cooldownKey);
                return Json(new { success = true, message, devCode, cooldown = CodeCooldownSeconds });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mengirim kode 2FA setup untuk user {UserId}", user.Id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyContactPhone(string code, string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["ContactError"] = "Kode OTP wajib diisi.";
                return RedirectToAction(nameof(ContactVerification), new { returnUrl });
            }

            try
            {
                await _accountService.ConfirmPhoneAsync(
                    user.Id,
                    code,
                    HttpContext.RequestAborted
                );
                TempData["ContactSuccess"] = "Nomor HP berhasil diverifikasi.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Verifikasi nomor HP gagal untuk user {UserId}", user.Id);
                TempData["ContactError"] = ex.Message;
            }

            return RedirectToAction(nameof(ContactVerification), new { returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");
            }

            model.Method = user.TwoFactorMethod;
            model.ReturnUrl = returnUrl ?? model.ReturnUrl;
            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var code = model.TwoFactorCode.Replace(" ", string.Empty, StringComparison.Ordinal);
            SignInResult result;

            if (user.TwoFactorMethod == TwoFactorMethod.AuthenticatorApp)
            {
                result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                    code,
                    model.RememberMe,
                    model.RememberMachine
                );
            }
            else
            {
                var provider = user.TwoFactorMethod == TwoFactorMethod.Email
                    ? TokenOptions.DefaultEmailProvider
                    : TokenOptions.DefaultPhoneProvider;

                result = await _signInManager.TwoFactorSignInAsync(
                    provider,
                    code,
                    model.RememberMe,
                    model.RememberMachine
                );
            }

            if (result.Succeeded)
            {
                await CompleteInteractiveLoginAsync(user, model.RememberMe, returnUrl);
                var checkpointRedirect = RedirectIfContactVerificationRequired(user, returnUrl);
                if (checkpointRedirect != null)
                    return checkpointRedirect;
                var setupRedirect = RedirectIfTwoFactorSetupRequired(user, returnUrl);
                if (setupRedirect != null)
                    return setupRedirect;
                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Akun Anda terkunci. Coba lagi nanti.");
                return View(model);
            }

            await _accountService.LogEventAsync(
                user.Id,
                SecurityLogEventType.LoginFailed,
                false,
                "Kode 2FA tidak valid.",
                GetRemoteIp(),
                GetUserAgent(),
                HttpContext.RequestAborted
            );
            ModelState.AddModelError(string.Empty, "Kode MFA tidak valid.");
            return View(model);
        }

        public async Task<IActionResult> LoginWithRecoveryCode(bool? rememberMe = true, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");
            }

            var model = new LoginWithRecoveryCodeViewModel
            {
                ReturnUrl = returnUrl,
                RememberMe = rememberMe ?? true,
            };
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Tidak dapat memuat pengguna 2FA.");
            }

            var recoveryCode = NormalizeRecoveryCode(model.RecoveryCode);
            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                HttpContext.Session.SetString(RecoveryResetSessionKey, "1");
                await CompleteInteractiveLoginAsync(user, model.RememberMe, returnUrl);
                return RedirectToAction(nameof(RecoveryReset));
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Akun Anda terkunci. Coba lagi nanti.");
                return View(model);
            }

            await _accountService.LogEventAsync(
                user.Id,
                SecurityLogEventType.LoginFailed,
                false,
                "Recovery code tidak valid.",
                GetRemoteIp(),
                GetUserAgent(),
                HttpContext.RequestAborted
            );
            ModelState.AddModelError(string.Empty, "Recovery code tidak valid.");
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            PopulateForgotPasswordCooldowns();
            return ForgotPasswordView("recovery");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordWithRecovery([Bind(Prefix = "Recovery")] ForgotPasswordRecoveryViewModel model)
        {
            if (!ModelState.IsValid)
                return ForgotPasswordView("recovery", recovery: model);

            var user = await FindUserAsync(model.Identifier);
            if (user == null)
            {
                ModelState.AddModelError("Recovery.Identifier", "Akun tidak ditemukan.");
                return ForgotPasswordView("recovery", recovery: model);
            }

            try
            {
                await _accountService.ResetPasswordWithRecoveryCodeAsync(
                    user.Id,
                    model.RecoveryCode,
                    model.NewPassword,
                    HttpContext.RequestAborted
                );
                TempData["SuccessMessage"] = "Password berhasil direset. Silakan login.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Recovery.RecoveryCode", ex.Message);
                return ForgotPasswordView("recovery", recovery: model);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmailResetCode([Bind(Prefix = "EmailReset")] ForgotPasswordResetWithCodeViewModel model)
        {
            ModelState.Remove("EmailReset.Code");
            ModelState.Remove("EmailReset.NewPassword");
            ModelState.Remove("EmailReset.ConfirmPassword");
            var isAjax = IsAjaxRequest();
            if (!ModelState.IsValid)
            {
                if (isAjax)
                    return AjaxModelError("Email tidak valid.");

                return ForgotPasswordView("email", emailReset: model);
            }

            if (IsCooldownActive(ForgotEmailCooldownKey, out var remaining))
            {
                if (isAjax)
                    return CooldownJsonResult(remaining);

                ModelState.AddModelError(string.Empty, $"Tunggu {remaining} detik sebelum mengirim ulang kode.");
                return ForgotPasswordView("email", emailReset: model);
            }

            string? devCode = null;
            var user = await FindUserAsync(model.Identifier);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var code = await _accountService.GeneratePasswordResetEmailCodeAsync(
                    user.Id,
                    HttpContext.RequestAborted
                );
                await _accountService.SendPasswordResetEmailCodeAsync(
                    user.Id,
                    code,
                    HttpContext.RequestAborted
                );
                if (_emailOptions.UseDevelopmentMode)
                {
                    devCode = code;
                    TempData["DevEmailResetCode"] = code;
                }
            }

            var message = "Jika akun ditemukan, kode reset dikirim ke email terdaftar.";
            TempData["SuccessMessage"] = message;
            StartCooldown(ForgotEmailCooldownKey);
            if (isAjax)
            {
                return Ok(new { success = true, message, devCode, cooldown = CodeCooldownSeconds });
            }
            return ForgotPasswordView("email", emailReset: model, emailCodeOverride: devCode);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordWithEmail([Bind(Prefix = "EmailReset")] ForgotPasswordResetWithCodeViewModel model)
        {
            if (!ModelState.IsValid)
                return ForgotPasswordView("email", emailReset: model);

            var user = await FindUserAsync(model.Identifier);
            if (user == null)
            {
                ModelState.AddModelError("EmailReset.Identifier", "Akun tidak ditemukan.");
                return ForgotPasswordView("email", emailReset: model);
            }

            try
            {
                await _accountService.ResetPasswordWithEmailCodeAsync(
                    user.Id,
                    model.Code,
                    model.NewPassword,
                    HttpContext.RequestAborted
                );
                TempData["SuccessMessage"] = "Password berhasil direset. Silakan login.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("EmailReset.Code", ex.Message);
                return ForgotPasswordView("email", emailReset: model);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendSmsResetCode([Bind(Prefix = "SmsReset")] ForgotPasswordResetWithCodeViewModel model)
        {
            ModelState.Remove("SmsReset.Code");
            ModelState.Remove("SmsReset.NewPassword");
            ModelState.Remove("SmsReset.ConfirmPassword");
            var isAjax = IsAjaxRequest();
            if (!ModelState.IsValid)
            {
                if (isAjax)
                    return AjaxModelError("Nomor HP tidak valid.");

                return ForgotPasswordView("sms", smsReset: model);
            }

            if (IsCooldownActive(ForgotSmsCooldownKey, out var smsRemaining))
            {
                if (isAjax)
                    return CooldownJsonResult(smsRemaining);

                ModelState.AddModelError(string.Empty, $"Tunggu {smsRemaining} detik sebelum mengirim ulang kode.");
                return ForgotPasswordView("sms", smsReset: model);
            }

            string? devCode = null;
            var user = await FindUserByPhoneAsync(model.Identifier);
            if (user == null)
            {
                var notFoundMessage = "Nomor HP tidak ditemukan.";
                TempData["ErrorMessage"] = notFoundMessage;
                if (isAjax)
                    return BadRequest(new { success = false, message = notFoundMessage });

                TempData["ErrorMessage"] = notFoundMessage;
                return ForgotPasswordView("sms", smsReset: model);
            }

            var code = await _accountService.GeneratePasswordResetSmsCodeAsync(
                user.Id,
                HttpContext.RequestAborted
            );
            await _accountService.SendPasswordResetSmsCodeAsync(
                user.Id,
                code,
                HttpContext.RequestAborted
            );

            if (_smsOptions.UseDevelopmentMode)
            {
                devCode = code;
                TempData["DevSmsResetCode"] = code;
            }

            var message = "Kode reset dikirim via SMS.";
            TempData["SuccessMessage"] = message;
            StartCooldown(ForgotSmsCooldownKey);
            if (isAjax)
            {
                return Ok(new { success = true, message, devCode, cooldown = CodeCooldownSeconds });
            }

            return ForgotPasswordView("sms", smsReset: model, smsCodeOverride: devCode);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordWithSms([Bind(Prefix = "SmsReset")] ForgotPasswordResetWithCodeViewModel model)
        {
            if (!ModelState.IsValid)
                return ForgotPasswordView("sms", smsReset: model);

            var user = await FindUserByPhoneAsync(model.Identifier);
            if (user == null)
            {
                ModelState.AddModelError("SmsReset.Identifier", "Nomor HP tidak ditemukan.");
                return ForgotPasswordView("sms", smsReset: model);
            }

            try
            {
                await _accountService.ResetPasswordWithSmsCodeAsync(
                    user.Id,
                    model.Code,
                    model.NewPassword,
                    HttpContext.RequestAborted
                );
                TempData["SuccessMessage"] = "Password berhasil direset. Silakan login.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("SmsReset.Code", ex.Message);
                return ForgotPasswordView("sms", smsReset: model);
            }
        }

        [Authorize]
        public IActionResult RecoveryReset()
        {
            if (!NeedsRecoveryReset())
                return RedirectToAction("Index", "Dashboard");

            return View(new RecoveryResetViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecoveryReset(RecoveryResetViewModel model)
        {
            if (!NeedsRecoveryReset())
                return RedirectToAction("Index", "Dashboard");

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Sesi berakhir. Silakan login ulang.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                await _accountService.ResetPasswordForUserAsync(
                    user.Id,
                    model.NewPassword,
                    "Password diperbarui setelah login dengan recovery code.",
                    HttpContext.RequestAborted
                );
                HttpContext.Session.Remove(RecoveryResetSessionKey);
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Password berhasil diperbarui.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal reset password setelah login recovery.");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTwoFactorCode()
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return Json(new { success = false, message = "Sesi 2FA tidak ditemukan." });
            }

            if (user.TwoFactorMethod == TwoFactorMethod.AuthenticatorApp)
            {
                return Json(new { success = false, message = "Authenticator tidak membutuhkan kode tambahan." });
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

            var devCode =
                user.TwoFactorMethod == TwoFactorMethod.Email && _emailOptions.UseDevelopmentMode
                    ? code
                    : user.TwoFactorMethod == TwoFactorMethod.Sms && _smsOptions.UseDevelopmentMode
                        ? code
                        : null;
            var channel = user.TwoFactorMethod == TwoFactorMethod.Email ? "email" : "SMS";
            var message = $"Kode verifikasi dikirim melalui {channel}.";

            if (devCode == null)
            {
                _logger.LogInformation(
                    "Kode 2FA dikirim via {Channel} untuk user {User}",
                    channel,
                    user.UserName
                );
            }
            else
            {
                _logger.LogInformation(
                    "2FA code (dev) untuk user {User}: {Code}",
                    user.UserName,
                    devCode
                );
            }

            return Json(new { success = true, message, devCode });
        }

        /*
         * POST: Logout
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            var sessionId = Request.Cookies[CookieNames.Session];

            if (user?.Id is string userId && !string.IsNullOrWhiteSpace(sessionId))
            {
                await _accountService.DeactivateSessionAsync(
                    userId,
                    sessionId,
                    HttpContext.RequestAborted
                );
                await _accountService.LogEventAsync(
                    userId,
                    SecurityLogEventType.Logout,
                    true,
                    "Logout manual dari perangkat.",
                    GetRemoteIp(),
                    GetUserAgent(),
                    HttpContext.RequestAborted
                );
            }

            ClearSessionCookie();
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out");

            return RedirectToAction("Index", "Dashboard");
        }

        /*
         * GET: Access Denied
         */
        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Refresh()
        {
            var user = await _userManager.GetUserAsync(User);
            await _signInManager.RefreshSignInAsync(user!); // ? rebuild principal + cookie
            return RedirectToAction("Index", "Dashboard");
        }

        private void PopulateForgotPasswordDevHints(string? emailCodeOverride = null, string? smsCodeOverride = null)
        {
            if (_emailOptions.UseDevelopmentMode && !string.IsNullOrWhiteSpace(emailCodeOverride))
            {
                TempData["DevEmailResetCode"] = emailCodeOverride;
            }

            if (_smsOptions.UseDevelopmentMode && !string.IsNullOrWhiteSpace(smsCodeOverride))
            {
                TempData["DevSmsResetCode"] = smsCodeOverride;
            }

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

        private IActionResult AjaxModelError(string? fallbackMessage = null)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(
                    x =>
                        new
                        {
                            field = x.Key,
                            messages = x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                        }
                )
                .ToArray();

            return BadRequest(
                new
                {
                    success = false,
                    message = fallbackMessage ?? "Permintaan tidak valid.",
                    errors
                }
            );
        }

        private bool IsAjaxRequest()
        {
            var headers = Request?.Headers;
            if (headers is null)
                return false;

            if (headers.TryGetValue("X-Requested-With", out var requestedWith)
                && requestedWith == "XMLHttpRequest")
            {
                return true;
            }

            foreach (var accept in headers!["Accept"])
            {
                if (!string.IsNullOrEmpty(accept)
                    && accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
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
            return View("ForgotPassword", BuildForgotPasswordPage(activeTab, recovery, emailReset, smsReset));
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

        private static string NormalizePhone(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var digitsOnly = new string(raw.Where(char.IsDigit).ToArray());
            if (digitsOnly.StartsWith("62"))
                digitsOnly = digitsOnly[2..];
            if (digitsOnly.StartsWith("0"))
                digitsOnly = digitsOnly[1..];

            return string.IsNullOrEmpty(digitsOnly) ? string.Empty : $"+62{digitsOnly}";
        }

        private bool NeedsRecoveryReset() => HttpContext.Session.GetString(RecoveryResetSessionKey) == "1";

        private async Task<User?> FindUserAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            var normalized = identifier.Trim();
            User? user = null;

            if (normalized.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(normalized);
            }

            if (user == null)
            {
                user = await _userManager.FindByNameAsync(normalized);
            }

            return user;
        }

        private async Task<User?> FindUserByPhoneAsync(string rawPhone)
        {
            if (string.IsNullOrWhiteSpace(rawPhone))
                return null;

            var normalized = NormalizePhone(rawPhone);
            var digits = new string(normalized.Where(char.IsDigit).ToArray());
            var candidates = new[]
            {
                normalized,
                $"+{digits}",
                "0" + digits,
                digits
            };

            return await _userManager.Users.FirstOrDefaultAsync(
                u => candidates.Contains(u.PhoneNumber ?? string.Empty)
            );
        }

        private async Task CompleteInteractiveLoginAsync(
            User user,
            bool rememberMe,
            string? returnUrl
        )
        {
            user.LastLoginAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            await _userManager.UpdateAsync(user);

            var userAgent = GetUserAgent();
            var ip = GetRemoteIp();
            var (device, browser) = ClientInfoHelper.Parse(userAgent);

            var session = await _accountService.RegisterSessionAsync(
                user.Id,
                userAgent,
                ip,
                device,
                browser,
                location: null,
                isCurrent: true,
                HttpContext.RequestAborted
            );

            SetSessionCookie(session.UserSessionId, rememberMe);

            await _accountService.LogEventAsync(
                user.Id,
                SecurityLogEventType.LoginSuccess,
                true,
                "Login interaktif berhasil.",
                ip,
                userAgent,
                HttpContext.RequestAborted
            );

            // intentionally no redirect: caller will handle navigation
        }

        private string? GetRemoteIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private string? GetUserAgent() => Request.Headers["User-Agent"].ToString();

        private void SetSessionCookie(string sessionId, bool persistent)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
            };

            if (persistent)
            {
                options.Expires = DateTimeOffset.UtcNow.AddDays(14);
            }

            Response.Cookies.Append(CookieNames.Session, sessionId, options);
        }

        private void ClearSessionCookie()
        {
            Response.Cookies.Delete(CookieNames.Session);
        }

        private bool IsCooldownActive(string key, out int remainingSeconds)
        {
            remainingSeconds = CooldownHelper.GetRemainingSeconds(HttpContext.Session, key);
            return remainingSeconds > 0;
        }

        private void StartCooldown(string key) =>
            CooldownHelper.SetCooldown(HttpContext.Session, key, TimeSpan.FromSeconds(CodeCooldownSeconds));

        private int GetCooldownSeconds(string key) =>
            CooldownHelper.GetRemainingSeconds(HttpContext.Session, key);

        private JsonResult CooldownJsonResult(int remaining) =>
            Json(new
            {
                success = false,
                message = $"Tunggu {remaining} detik sebelum mengirim ulang.",
                remaining
            });

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
            !string.IsNullOrWhiteSpace(user.PhoneNumber) && !user.PhoneNumberConfirmed;

        private static string GetTabForMethod(TwoFactorMethod method) =>
            method switch
            {
                TwoFactorMethod.Email => "email",
                TwoFactorMethod.Sms => "sms",
                _ => "auth",
            };

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
    }
}
