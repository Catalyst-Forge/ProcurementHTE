using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Models.Enums;
using ProcurementHTE.Core.Options;
using ProcurementHTE.Web.Constants;
using ProcurementHTE.Web.Helpers;
using ProcurementHTE.Web.Models.Account;

namespace ProcurementHTE.Web.Controllers.Account
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IAccountService _accountService;
        private readonly EmailSenderOptions _emailOptions;
        private readonly SmsSenderOptions _smsOptions;
        private readonly ILogger<AccountController> _logger;
        private const int VerificationCooldownSeconds = 60;
        private const string EmailVerificationCooldownKey = "settings.email";
        private const string PhoneVerificationCooldownKey = "settings.phone";

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IAccountService accountService,
            IOptions<EmailSenderOptions> emailOptions,
            IOptions<SmsSenderOptions> smsOptions,
            ILogger<AccountController> logger
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _accountService = accountService;
            _emailOptions = emailOptions.Value;
            _smsOptions = smsOptions.Value;
            _logger = logger;
        }

        public async Task<IActionResult> Settings()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            var overview = await _accountService.GetOverviewAsync(user.Id, HttpContext.RequestAborted);
            var twoFactor = await _accountService.GetTwoFactorSummaryAsync(user.Id, HttpContext.RequestAborted);
            var sessions = await _accountService.GetSessionsAsync(user.Id, HttpContext.RequestAborted);
            sessions = sessions.Where(s => s.IsActive || s.IsCurrent).ToList();
            var logs = await _accountService.GetSecurityLogsAsync(user.Id, 5, HttpContext.RequestAborted);
            var currentSessionId = GetCurrentSessionId();
            var recoveryCodes = overview.RecoveryCodesSnapshot?.ToArray();
            var profilePhoneInput = FormatPhoneForInput(overview.PhoneNumber);

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
                    .Select(
                        x =>
                            new UserSessionViewModel
                            {
                                SessionId = x.UserSessionId,
                                Device = x.Device ?? "Tidak diketahui",
                                Browser = x.Browser ?? "Tidak diketahui",
                                IpAddress = x.IpAddress,
                                Location = x.Location,
                                CreatedAt = x.CreatedAt,
                                IsActive = x.IsActive,
                                IsCurrent = x.UserSessionId == currentSessionId,
                            }
                    )
                    .ToList(),
                SecurityLogs = logs,
            };

            ViewBag.ActivePage = "settings";
            ViewBag.ShowPhoneVerify = TempData["ShowPhoneVerify"]?.ToString() == "1";
            ViewBag.DevPhoneOtp = _smsOptions.UseDevelopmentMode ? TempData["DevPhoneOtp"] : null;
            ViewBag.DevMagicLink = _emailOptions.UseDevelopmentMode ? TempData["DevMagicLink"] : null;
            ViewBag.RecoveryCodesHidden = overview.RecoveryCodesHidden;
            ViewBag.HasStoredRecoveryCodes = recoveryCodes?.Length > 0;
            ViewBag.EmailVerificationCooldown = GetCooldownSeconds(EmailVerificationCooldownKey);
            ViewBag.PhoneVerificationCooldown = GetCooldownSeconds(PhoneVerificationCooldownKey);
            ViewBag.RequirePhoneVerificationForTwoFactor =
                twoFactor.IsEnabled
                && twoFactor.Method == TwoFactorMethod.Sms
                && !overview.PhoneNumberConfirmed;
            ViewBag.RequireEmailVerificationForTwoFactor =
                twoFactor.IsEnabled
                && twoFactor.Method == TwoFactorMethod.Email
                && !overview.EmailConfirmed;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Profile")] UpdateProfileInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Periksa kembali data profil yang Anda masukkan.";
                return RedirectToAction(nameof(Settings));
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            var normalizedEmail = model.Email?.Trim() ?? string.Empty;
            var normalizedPhone = NormalizePhoneInput(model.PhoneNumber);
            var currentPhone = string.IsNullOrWhiteSpace(user.PhoneNumber)
                ? null
                : user.PhoneNumber.Trim();
            var emailChanged = !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
            var phoneChanged = !string.Equals(currentPhone, normalizedPhone, StringComparison.OrdinalIgnoreCase);
            var requiresPhoneVerification = phoneChanged && !string.IsNullOrWhiteSpace(normalizedPhone);

            var request = new UpdateProfileRequest
            {
                UserId = user.Id,
                FirstName = model.FirstName,
                LastName = model.LastName ?? string.Empty,
                JobTitle = model.JobTitle,
                Email = normalizedEmail,
                UserName = model.UserName,
                PhoneNumber = normalizedPhone,
            };

            try
            {
                await _accountService.UpdateProfileAsync(request, HttpContext.RequestAborted);

                if (emailChanged || requiresPhoneVerification)
                {
                    TempData["ContactSuccess"] =
                        "Profil berhasil diperbarui. Silakan verifikasi kontak yang baru diperbarui.";
                    var returnUrl = Url.Action(nameof(Settings), "Account") ?? "/";
                    return RedirectToAction("ContactVerification", "Auth", new { returnUrl });
                }

                TempData["SuccessMessage"] = "Profil berhasil diperbarui.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal memperbarui profil user {UserId}", user.Id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(
                    "<br/>",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                );
                return RedirectToAction(nameof(Settings));
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _accountService.ChangePasswordAsync(
                    user.Id,
                    model.CurrentPassword,
                    model.NewPassword,
                    HttpContext.RequestAborted
                );
                TempData["SuccessMessage"] = "Password berhasil diperbarui.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mengganti password user {UserId}", user.Id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmailVerification()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                var encodedToken = await _accountService.GenerateEmailVerificationTokenAsync(
                    user.Id,
                    HttpContext.RequestAborted
                );
                var callbackUrl = Url.Action(
                    nameof(ConfirmEmail),
                    "Account",
                    new { userId = user.Id, token = encodedToken },
                    Request.Scheme
                );

                if (string.IsNullOrWhiteSpace(callbackUrl))
                {
                    TempData["ErrorMessage"] = "Tidak dapat membuat tautan verifikasi saat ini.";
                    return RedirectToAction(nameof(Settings));
                }

                await _accountService.SendEmailVerificationAsync(
                    user.Id,
                    callbackUrl,
                    HttpContext.RequestAborted
                );

                if (_emailOptions.UseDevelopmentMode)
                {
                    TempData["DevMagicLink"] = callbackUrl;
                }
                TempData["SuccessMessage"] =
                    $"Magic link verifikasi telah kami kirim ke {user.Email}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal mengirim email verifikasi untuk {UserId}", user.Id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Settings));
        }

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Tautan tidak valid.";
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                await _accountService.ConfirmEmailAsync(
                    userId,
                    token,
                    HttpContext.RequestAborted
                );
                TempData["SuccessMessage"] = "Email Anda telah terverifikasi.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Verifikasi email gagal untuk user {UserId}", userId);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPhoneVerification()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            if (IsCooldownActive(PhoneVerificationCooldownKey, out var phoneRemaining))
            {
                TempData["ErrorMessage"] = $"Tunggu {phoneRemaining} detik sebelum mengirim ulang OTP.";
                TempData["ShowPhoneVerify"] = "1";
                return RedirectToAction(nameof(Settings));
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
                TempData["ShowPhoneVerify"] = "1";
                if (_smsOptions.UseDevelopmentMode)
                {
                    TempData["DevPhoneOtp"] = code;
                }
                TempData["SuccessMessage"] =
                    $"Kode OTP telah dikirim ke {user.PhoneNumber ?? "nomor HP Anda"}.";
                StartCooldown(PhoneVerificationCooldownKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Gagal mengirim OTP verifikasi nomor HP user {UserId}",
                    user?.Id
                );
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhone(string code)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["ErrorMessage"] = "Kode OTP wajib diisi.";
                TempData["ShowPhoneVerify"] = "1";
                return RedirectToAction(nameof(Settings));
            }

            try
            {
                await _accountService.ConfirmPhoneAsync(
                    user.Id,
                    code,
                    HttpContext.RequestAborted
                );
                TempData["SuccessMessage"] = "Nomor HP berhasil diverifikasi.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Verifikasi nomor HP gagal untuk {UserId}", user.Id);
                TempData["ErrorMessage"] = ex.Message;
                TempData["ShowPhoneVerify"] = "1";
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvatar(UpdateAvatarInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Pilih gambar valid sebelum menyimpan.";
                return RedirectToAction(nameof(Settings));
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                var (buffer, contentType, fileName) = ParseImagePayload(model.ImageData);
                await using var stream = new MemoryStream(buffer, writable: false);
                var request = new UploadAvatarRequest
                {
                    UserId = user.Id,
                    Content = stream,
                    FileName = fileName,
                    ContentType = contentType,
                    Length = stream.Length,
                };

                await _accountService.UploadAvatarAsync(request, HttpContext.RequestAborted);
                TempData["SuccessMessage"] = "Foto profil diperbarui.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal mengunggah avatar user {UserId}", user?.Id);
                TempData["ErrorMessage"] = "Gagal mengunggah foto profil. Pastikan format gambar valid.";
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvatar()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            await _accountService.RemoveAvatarAsync(user.Id, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Foto profil dihapus.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor(TwoFactorMethod method, string verificationCode)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

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
                _logger.LogWarning(ex, "Gagal mengaktifkan 2FA user {UserId}", user.Id);
                TempData["ErrorMessage"] = ex.Message;
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
                _logger.LogWarning(ex, "Gagal menonaktifkan 2FA user {UserId}", user.Id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            var codes = await _accountService.GenerateRecoveryCodesAsync(
                user.Id,
                HttpContext.RequestAborted
            );

            TempData["SuccessMessage"] = "Recovery codes baru berhasil dibuat.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadRecoveryCodes()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            var codes = await _accountService.GetRecoveryCodesSnapshotAsync(
                user.Id,
                HttpContext.RequestAborted
            );

            if (codes == null || codes.Count == 0)
            {
                TempData["ErrorMessage"] = "Belum ada recovery code yang siap diunduh.";
                return RedirectToAction(nameof(Settings));
            }

            var builder = new StringBuilder();
            builder.AppendLine("Procurement HTE - Recovery Codes");
            builder.AppendLine("Simpan file ini di tempat aman. Jangan bagikan ke siapapun.");
            builder.AppendLine();
            foreach (var code in codes)
            {
                builder.AppendLine(code);
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var fileName = $"recovery-codes-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
            return File(bytes, "text/plain", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideRecoveryCodes()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            await _accountService.SetRecoveryCodesHiddenAsync(
                user.Id,
                true,
                HttpContext.RequestAborted
            );

            TempData["SuccessMessage"] = "Recovery codes disembunyikan dari layar.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShowRecoveryCodes()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            await _accountService.SetRecoveryCodesHiddenAsync(
                user.Id,
                false,
                HttpContext.RequestAborted
            );

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
                return Json(new { success = false, message = "Authenticator tidak membutuhkan kode pengiriman." });
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

                if (devCode == null)
                {
                    _logger.LogInformation(
                        "2FA code dikirim via {Channel} untuk user {UserId}",
                        channel,
                        user.Id
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "2FA code (dev) untuk user {UserId}: {Code}",
                        user.Id,
                        devCode
                    );
                }

                return Json(new { success = true, message, devCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal mengirim kode 2FA ke user {UserId}", user.Id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutAllSessions()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            await _accountService.DeactivateAllSessionsAsync(user.Id, HttpContext.RequestAborted);
            await _accountService.LogEventAsync(
                user.Id,
                SecurityLogEventType.LogoutAllSessions,
                true,
                "Logout dari semua perangkat.",
                GetRemoteIp(),
                GetUserAgent(),
                HttpContext.RequestAborted
            );

            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            var (device, browser) = ClientInfoHelper.Parse(GetUserAgent());
            var newSession = await _accountService.RegisterSessionAsync(
                user.Id,
                GetUserAgent(),
                GetRemoteIp(),
                device,
                browser,
                null,
                true,
                HttpContext.RequestAborted
            );
            SetSessionCookie(newSession.UserSessionId);

            TempData["SuccessMessage"] = "Semua sesi berhasil di-logout.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return RedirectToAction(nameof(Settings));

            var user = await GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            var currentSession = GetCurrentSessionId();
            if (currentSession == sessionId)
            {
                TempData["ErrorMessage"] = "Tidak dapat menghapus sesi yang sedang digunakan.";
                return RedirectToAction(nameof(Settings));
            }

            await _accountService.DeactivateSessionAsync(
                user.Id,
                sessionId,
                HttpContext.RequestAborted
            );
            TempData["SuccessMessage"] = "Sesi berhasil dihentikan.";
            return RedirectToAction(nameof(Settings));
        }

        private static string? NormalizePhoneInput(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var digitsOnly = new string(raw.Where(char.IsDigit).ToArray());
            if (digitsOnly.StartsWith("62"))
                digitsOnly = digitsOnly[2..];
            if (digitsOnly.StartsWith("0"))
                digitsOnly = digitsOnly[1..];

            return string.IsNullOrEmpty(digitsOnly) ? null : $"+62{digitsOnly}";
        }

        private static string? FormatPhoneForInput(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
                return null;

            var normalized = stored.Trim();
            if (normalized.StartsWith("+62"))
                normalized = normalized[3..];
            else if (normalized.StartsWith("62"))
                normalized = normalized[2..];
            if (normalized.StartsWith("0"))
                normalized = normalized[1..];

            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private Task<User?> GetCurrentUserAsync() => _userManager.GetUserAsync(User);

        private string? GetCurrentSessionId() => Request.Cookies[CookieNames.Session];

        private string? GetRemoteIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private bool IsCooldownActive(string key, out int remainingSeconds)
        {
            remainingSeconds = CooldownHelper.GetRemainingSeconds(HttpContext.Session, key);
            return remainingSeconds > 0;
        }

        private void StartCooldown(string key) =>
            CooldownHelper.SetCooldown(HttpContext.Session, key, TimeSpan.FromSeconds(VerificationCooldownSeconds));

        private int GetCooldownSeconds(string key) =>
            CooldownHelper.GetRemainingSeconds(HttpContext.Session, key);

        private string? GetUserAgent() => Request.Headers["User-Agent"].ToString();

        private void SetSessionCookie(string sessionId)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(14),
            };

            Response.Cookies.Append(CookieNames.Session, sessionId, options);
        }

        private static (byte[] Buffer, string ContentType, string FileName) ParseImagePayload(string base64)
        {
            var span = base64.AsSpan();
            var commaIndex = span.IndexOf(',');
            ReadOnlySpan<char> metadata = span;
            ReadOnlySpan<char> payload = span;

            if (commaIndex >= 0)
            {
                metadata = span[..commaIndex];
                payload = span[(commaIndex + 1)..];
            }

            var contentType = metadata
                .ToString()
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(part => part.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                ?.Replace("data:", string.Empty, StringComparison.OrdinalIgnoreCase)
                ?? "image/png";

            var buffer = Convert.FromBase64String(payload.ToString());
            var extension = contentType switch
            {
                "image/png" => "png",
                "image/jpeg" => "jpg",
                _ => "png"
            };

            return (buffer, contentType, $"avatar.{extension}");
        }
    }
}
