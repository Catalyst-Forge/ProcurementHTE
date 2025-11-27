using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Models.Enums;
using QRCoder;

namespace ProcurementHTE.Core.Services
{
    public class AccountService : IAccountService
    {
        private const string PasswordResetPurpose = "PasswordReset";
        private readonly UserManager<User> _userManager;
        private readonly IObjectStorage _objectStorage;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IUserSecurityLogRepository _logRepository;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public AccountService(
            UserManager<User> userManager,
            IObjectStorage objectStorage,
            IOptions<ObjectStorageOptions> storageOptions,
            IUserSessionRepository sessionRepository,
            IUserSecurityLogRepository logRepository,
            IEmailSender emailSender,
            ISmsSender smsSender
        )
        {
            _userManager = userManager;
            _objectStorage =
                objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
            _storageOptions =
                storageOptions?.Value ?? throw new ArgumentNullException(nameof(storageOptions));
            _sessionRepository = sessionRepository;
            _logRepository = logRepository;
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
            _smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));

            if (string.IsNullOrWhiteSpace(_storageOptions.Bucket))
                throw new ArgumentException(
                    "Object storage bucket belum dikonfigurasi.",
                    nameof(storageOptions)
                );
        }

        public async Task<AccountOverviewDto> GetOverviewAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            var roleList = roles?.ToList() ?? new List<string>();

            return new AccountOverviewDto(
                user.Id,
                user.UserName!,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.JobTitle,
                user.PhoneNumber,
                await BuildAvatarUrlAsync(user, ct),
                user.PhoneNumberConfirmed,
                user.EmailConfirmed,
                await _userManager.GetTwoFactorEnabledAsync(user),
                user.TwoFactorMethod,
                user.CreatedAt,
                user.UpdatedAt,
                user.LastLoginAt,
                user.PasswordChangedAt,
                roleList,
                ParseRecoveryCodes(user.RecoveryCodesJson),
                user.RecoveryCodesHidden,
                user.RecoveryCodesGeneratedAt
            );
        }

        public async Task UpdateProfileAsync(
            UpdateProfileRequest request,
            CancellationToken ct = default
        )
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = await RequireUserAsync(request.UserId);

            var emailChanged = !string.Equals(
                user.Email,
                request.Email,
                StringComparison.OrdinalIgnoreCase
            );
            var phoneChanged = !string.Equals(
                user.PhoneNumber,
                request.PhoneNumber,
                StringComparison.OrdinalIgnoreCase
            );

            if (!string.Equals(user.UserName, request.UserName, StringComparison.OrdinalIgnoreCase))
            {
                var userNameResult = await _userManager.SetUserNameAsync(user, request.UserName);
                EnsureSucceeded(userNameResult, "Gagal memperbarui username.");
            }

            if (emailChanged)
            {
                var emailResult = await _userManager.SetEmailAsync(user, request.Email);
                EnsureSucceeded(emailResult, "Gagal memperbarui email.");
                user.EmailConfirmed = false;
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName ?? string.Empty;
            user.JobTitle = request.JobTitle;
            user.PhoneNumber = request.PhoneNumber;
            if (phoneChanged)
            {
                user.PhoneNumberConfirmed = false;
            }
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            EnsureSucceeded(updateResult, "Gagal memperbarui profil.");

            await LogEventAsync(
                user.Id,
                SecurityLogEventType.ProfileUpdated,
                true,
                "Profil dasar diperbarui.",
                null,
                null,
                ct
            );
        }

        public async Task<string?> UploadAvatarAsync(
            UploadAvatarRequest request,
            CancellationToken ct = default
        )
        {
            ArgumentNullException.ThrowIfNull(request);
            var user = await RequireUserAsync(request.UserId);

            if (request.Content.CanSeek)
                request.Content.Position = 0;

            var fileName = SanitizeFileName(request.FileName);
            var objectKey = $"avatars/{user.Id}/{Guid.NewGuid():N}-{fileName}";

            if (!string.IsNullOrWhiteSpace(user.AvatarObjectKey))
            {
                await SafeDeleteAsync(user.AvatarObjectKey);
            }

            await _objectStorage.UploadAsync(
                _storageOptions.Bucket,
                objectKey,
                request.Content,
                request.Length,
                request.ContentType,
                ct
            );

            user.AvatarObjectKey = objectKey;
            user.AvatarFileName = fileName;
            user.AvatarUpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            EnsureSucceeded(updateResult, "Gagal menyimpan foto profil.");

            await LogEventAsync(
                user.Id,
                SecurityLogEventType.AvatarUpdated,
                true,
                "Foto profil diperbarui.",
                null,
                null,
                ct
            );

            return await BuildAvatarUrlAsync(user, ct);
        }

        public async Task RemoveAvatarAsync(string userId, CancellationToken ct = default)
        {
            var user = await RequireUserAsync(userId);
            if (string.IsNullOrWhiteSpace(user.AvatarObjectKey))
                return;

            await SafeDeleteAsync(user.AvatarObjectKey);
            user.AvatarObjectKey = null;
            user.AvatarFileName = null;
            user.AvatarUpdatedAt = null;

            var updateResult = await _userManager.UpdateAsync(user);
            EnsureSucceeded(updateResult, "Gagal menghapus foto profil.");

            await LogEventAsync(
                user.Id,
                SecurityLogEventType.AvatarUpdated,
                true,
                "Foto profil dihapus.",
                null,
                null,
                ct
            );
        }

        public async Task ChangePasswordAsync(
            string userId,
            string currentPassword,
            string newPassword,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            EnsureSucceeded(result, "Gagal mengganti password.");

            user.PasswordChangedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await LogEventAsync(
                user.Id,
                SecurityLogEventType.PasswordChanged,
                true,
                "Password berhasil diperbarui.",
                null,
                null,
                ct
            );
        }

        public async Task<TwoFactorSummaryDto> GetTwoFactorSummaryAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            var isEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var recoveryCodes = await _userManager.CountRecoveryCodesAsync(user);
            var sharedKey = await GetAuthenticatorKeyAsync(user);
            var authUri = GenerateQrCodeUri(user.Email ?? user.UserName!, sharedKey);

            return new TwoFactorSummaryDto(
                isEnabled,
                user.TwoFactorMethod,
                recoveryCodes,
                sharedKey,
                authUri,
                BuildQrImage(authUri)
            );
        }

        public async Task<string> GenerateTwoFactorCodeAsync(
            string userId,
            TwoFactorMethod method,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            if (method == TwoFactorMethod.Sms && string.IsNullOrWhiteSpace(user.PhoneNumber))
                throw new InvalidOperationException("Lengkapi nomor HP sebelum mengirim kode SMS.");
            if (method == TwoFactorMethod.Email && string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException("Alamat email tidak tersedia.");
            if (method == TwoFactorMethod.Email && !user.EmailConfirmed)
                throw new InvalidOperationException(
                    "Verifikasi email terlebih dahulu sebelum memakai Email OTP."
                );
            if (method == TwoFactorMethod.Sms && !user.PhoneNumberConfirmed)
                throw new InvalidOperationException(
                    "Nomor HP harus terverifikasi sebelum memakai SMS OTP."
                );
            return method switch
            {
                TwoFactorMethod.Email => await _userManager.GenerateTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider
                ),
                TwoFactorMethod.Sms => await _userManager.GenerateTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultPhoneProvider
                ),
                _ => throw new InvalidOperationException(
                    "Metode ini tidak membutuhkan kode terpisah."
                ),
            };
        }

        public async Task SendTwoFactorCodeAsync(
            string userId,
            TwoFactorMethod method,
            string code,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            switch (method)
            {
                case TwoFactorMethod.Email:
                    if (string.IsNullOrWhiteSpace(user.Email))
                        throw new InvalidOperationException("Alamat email tidak tersedia.");
                    if (!user.EmailConfirmed)
                        throw new InvalidOperationException(
                            "Verifikasi email terlebih dahulu sebelum memakai Email OTP."
                        );
                    var body = new StringBuilder()
                        .AppendLine("<p>Gunakan kode berikut untuk verifikasi login Anda:</p>")
                        .AppendLine(
                            $"""<p style="font-size:22px;font-weight:bold;letter-spacing:4px;">{code}</p>"""
                        )
                        .AppendLine(
                            "<p>Kode hanya berlaku sementara. Jangan bagikan kepada siapapun.</p>"
                        )
                        .ToString();
                    await _emailSender.SendAsync(
                        user.Email,
                        "Kode Verifikasi Two-Factor Procurement HTE",
                        body,
                        ct
                    );
                    break;
                case TwoFactorMethod.Sms:
                    if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                        throw new InvalidOperationException("Nomor HP belum diisi.");
                    if (!user.PhoneNumberConfirmed)
                        throw new InvalidOperationException(
                            "Verifikasi nomor HP terlebih dahulu sebelum memakai SMS OTP."
                        );
                    await _smsSender.SendAsync(
                        user.PhoneNumber,
                        $"Kode verifikasi login Procurement HTE: {code}",
                        ct
                    );
                    break;
                default:
                    throw new InvalidOperationException(
                        "Metode ini tidak mendukung pengiriman kode."
                    );
            }
        }

        public async Task EnableTwoFactorAsync(
            string userId,
            TwoFactorMethod method,
            string verificationCode,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            if (method == TwoFactorMethod.Sms && string.IsNullOrWhiteSpace(user.PhoneNumber))
                throw new InvalidOperationException(
                    "Lengkapi nomor HP sebelum mengaktifkan 2FA SMS."
                );
            if (method == TwoFactorMethod.Email && string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException("Alamat email tidak tersedia.");
            if (method == TwoFactorMethod.Email && !user.EmailConfirmed)
                throw new InvalidOperationException(
                    "Verifikasi email terlebih dahulu sebelum mengaktifkan 2FA Email."
                );
            if (method == TwoFactorMethod.Sms && !user.PhoneNumberConfirmed)
                throw new InvalidOperationException(
                    "Verifikasi nomor HP terlebih dahulu sebelum mengaktifkan 2FA SMS."
                );
            var normalized = verificationCode?.Replace(" ", string.Empty);

            bool isValid = method switch
            {
                TwoFactorMethod.AuthenticatorApp => await _userManager.VerifyTwoFactorTokenAsync(
                    user,
                    _userManager.Options.Tokens.AuthenticatorTokenProvider,
                    normalized!
                ),
                TwoFactorMethod.Email => await _userManager.VerifyTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider,
                    normalized!
                ),
                TwoFactorMethod.Sms when !string.IsNullOrWhiteSpace(user.PhoneNumber) =>
                    await _userManager.VerifyTwoFactorTokenAsync(
                        user,
                        TokenOptions.DefaultPhoneProvider,
                        normalized!
                    ),
                _ => false,
            };

            if (!isValid)
            {
                await LogEventAsync(
                    user.Id,
                    SecurityLogEventType.TwoFactorMethodChanged,
                    false,
                    "Kode verifikasi 2FA tidak valid.",
                    null,
                    null,
                    ct
                );
                throw new InvalidOperationException("Kode verifikasi tidak valid.");
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            EnsureSucceeded(result, "Gagal mengaktifkan 2FA.");

            user.TwoFactorMethod = method;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await LogEventAsync(
                user.Id,
                SecurityLogEventType.TwoFactorEnabled,
                true,
                $"2FA diaktifkan dengan metode {method}.",
                null,
                null,
                ct
            );
        }

        public async Task DisableTwoFactorAsync(string userId, CancellationToken ct = default)
        {
            var user = await RequireUserAsync(userId);
            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            EnsureSucceeded(result, "Gagal menonaktifkan 2FA.");

            user.TwoFactorMethod = TwoFactorMethod.None;
            await _userManager.UpdateAsync(user);

            await LogEventAsync(
                user.Id,
                SecurityLogEventType.TwoFactorDisabled,
                true,
                "2FA dinonaktifkan.",
                null,
                null,
                ct
            );
        }

        public async Task<IEnumerable<string>> GenerateRecoveryCodesAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            var generated = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            var codes = generated?.ToArray() ?? Array.Empty<string>();
            user.RecoveryCodesJson = string.Join(';', codes);
            user.RecoveryCodesHidden = false;
            user.RecoveryCodesGeneratedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await LogEventAsync(
                user.Id,
                SecurityLogEventType.TwoFactorMethodChanged,
                true,
                "Recovery codes digenerate ulang.",
                null,
                null,
                ct
            );

            return codes;
        }

        public async Task<IReadOnlyList<string>?> GetRecoveryCodesSnapshotAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            return ParseRecoveryCodes(user.RecoveryCodesJson);
        }

        public async Task SetRecoveryCodesHiddenAsync(
            string userId,
            bool hidden,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            user.RecoveryCodesHidden = hidden;
            await _userManager.UpdateAsync(user);
        }

        public async Task ResetPasswordWithRecoveryCodeAsync(
            string userId,
            string recoveryCode,
            string newPassword,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            var normalized = NormalizeRecoveryCode(recoveryCode);
            var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, normalized);
            EnsureSucceeded(result, "Recovery code tidak valid.");
            await ResetPasswordInternalAsync(
                user,
                newPassword,
                "Password direset menggunakan recovery code.",
                ct
            );
        }

        public async Task<string> GeneratePasswordResetEmailCodeAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            return await _userManager.GenerateUserTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider,
                PasswordResetPurpose
            );
        }

        public async Task SendPasswordResetEmailCodeAsync(
            string userId,
            string code,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            if (string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException("Email belum diisi.");

            var body = new StringBuilder()
                .AppendLine("<p>Gunakan kode berikut untuk mereset password Anda:</p>")
                .AppendLine($"""<p style="font-size:22px;font-weight:bold;">{code}</p>""")
                .AppendLine("<p>Kode berlaku terbatas. Jangan bagikan ke siapapun.</p>")
                .ToString();

            await _emailSender.SendAsync(
                user.Email,
                "Kode Reset Password Procurement HTE",
                body,
                ct
            );
        }

        public async Task ResetPasswordWithEmailCodeAsync(
            string userId,
            string code,
            string newPassword,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            var valid = await _userManager.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider,
                PasswordResetPurpose,
                code
            );

            if (!valid)
                throw new InvalidOperationException(
                    "Kode email tidak valid atau sudah kadaluarsa."
                );

            await ResetPasswordInternalAsync(
                user,
                newPassword,
                "Password direset melalui kode email.",
                ct
            );
        }

        public async Task<string> GeneratePasswordResetSmsCodeAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                throw new InvalidOperationException("Nomor HP belum diisi.");

            return await _userManager.GenerateUserTokenAsync(
                user,
                TokenOptions.DefaultPhoneProvider,
                PasswordResetPurpose
            );
        }

        public async Task SendPasswordResetSmsCodeAsync(
            string userId,
            string code,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                throw new InvalidOperationException("Nomor HP belum diisi.");

            await _smsSender.SendAsync(
                user.PhoneNumber,
                $"Kode reset password Procurement HTE: {code}",
                ct
            );
        }

        public async Task ResetPasswordWithSmsCodeAsync(
            string userId,
            string code,
            string newPassword,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                throw new InvalidOperationException("Nomor HP belum diisi.");

            var valid = await _userManager.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultPhoneProvider,
                PasswordResetPurpose,
                code
            );

            if (!valid)
                throw new InvalidOperationException("Kode SMS tidak valid atau sudah kadaluarsa.");

            await ResetPasswordInternalAsync(
                user,
                newPassword,
                "Password direset melalui kode SMS.",
                ct
            );
        }

        public async Task ResetPasswordForUserAsync(
            string userId,
            string newPassword,
            string description,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            await ResetPasswordInternalAsync(user, newPassword, description, ct);
        }

        public async Task<string> GenerateEmailVerificationTokenAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            return EncodeToken(token);
        }

        public async Task SendEmailVerificationAsync(
            string userId,
            string callbackUrl,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException(
                    "Callback url tidak boleh kosong.",
                    nameof(callbackUrl)
                );

            var user = await RequireUserAsync(userId);
            if (string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException("Email pengguna belum diisi.");

            var subject = "Verifikasi Email Procurement HTE";
            var body = new StringBuilder()
                .AppendLine("<p>Halo,</p>")
                .AppendLine("<p>Klik tautan berikut untuk memverifikasi email Anda:</p>")
                .AppendLine(
                    $"""<p><a href="{callbackUrl}" style="font-weight:600;">Verifikasi Sekarang</a></p>"""
                )
                .AppendLine("<p>Abaikan jika Anda tidak meminta verifikasi ini.</p>")
                .ToString();

            await _emailSender.SendAsync(user.Email, subject, body, ct);
        }

        public async Task ConfirmEmailAsync(
            string userId,
            string encodedToken,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            var token = DecodeToken(encodedToken);
            var result = await _userManager.ConfirmEmailAsync(user, token);
            EnsureSucceeded(result, "Gagal memverifikasi email.");
            await LogEventAsync(
                user.Id,
                SecurityLogEventType.EmailVerified,
                true,
                "Email berhasil diverifikasi.",
                null,
                null,
                ct
            );
        }

        public async Task<string> GeneratePhoneVerificationCodeAsync(
            string userId,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                throw new InvalidOperationException("Nomor HP belum diisi.");

            return await _userManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
        }

        public async Task SendPhoneVerificationCodeAsync(
            string userId,
            string code,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                throw new InvalidOperationException("Nomor HP belum diisi.");

            var message = $"Kode verifikasi Procurement HTE: {code}";
            await _smsSender.SendAsync(user.PhoneNumber, message, ct);
        }

        public async Task ConfirmPhoneAsync(
            string userId,
            string code,
            CancellationToken ct = default
        )
        {
            var user = await RequireUserAsync(userId);
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                throw new InvalidOperationException("Nomor HP belum diisi.");

            var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, code);
            EnsureSucceeded(result, "Kode OTP tidak valid.");
            await LogEventAsync(
                user.Id,
                SecurityLogEventType.PhoneVerified,
                true,
                "Nomor HP berhasil diverifikasi.",
                null,
                null,
                ct
            );
        }

        public Task<IReadOnlyList<UserSession>> GetSessionsAsync(
            string userId,
            CancellationToken ct = default
        ) => _sessionRepository.GetByUserAsync(userId, ct);

        public async Task<UserSession> RegisterSessionAsync(
            string userId,
            string? userAgent,
            string? ipAddress,
            string? device,
            string? browser,
            string? location,
            bool isCurrent,
            CancellationToken ct = default
        )
        {
            var session = new UserSession
            {
                UserId = userId,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                Device = device,
                Browser = browser,
                Location = location,
                IsCurrent = isCurrent,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
            };

            await _sessionRepository.AddAsync(session, ct);
            await _sessionRepository.SaveAsync(ct);
            return session;
        }

        public async Task DeactivateSessionAsync(
            string userId,
            string sessionId,
            CancellationToken ct = default
        )
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, ct);
            if (session is null || session.UserId != userId)
                return;

            session.IsActive = false;
            session.IsCurrent = false;

            await _sessionRepository.UpdateAsync(session, ct);
            await _sessionRepository.SaveAsync(ct);

            await LogEventAsync(
                userId,
                SecurityLogEventType.SessionRevoked,
                true,
                $"Sesi {session.Browser ?? session.Device ?? session.UserAgent} direvoke.",
                session.IpAddress,
                session.UserAgent,
                ct
            );
        }

        public async Task DeactivateAllSessionsAsync(string userId, CancellationToken ct = default)
        {
            var sessions = await _sessionRepository.GetByUserAsync(userId, ct);
            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.IsCurrent = false;
                await _sessionRepository.UpdateAsync(session, ct);
            }

            await _sessionRepository.SaveAsync(ct);
        }

        public Task<IReadOnlyList<UserSecurityLog>> GetSecurityLogsAsync(
            string userId,
            int take,
            CancellationToken ct = default
        ) => _logRepository.GetRecentAsync(userId, take, ct);

        public async Task LogEventAsync(
            string userId,
            SecurityLogEventType eventType,
            bool success,
            string? description,
            string? ipAddress,
            string? userAgent,
            CancellationToken ct = default
        )
        {
            var log = new UserSecurityLog
            {
                UserId = userId,
                EventType = eventType,
                IsSuccess = success,
                Description = description,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
            };

            await _logRepository.AddAsync(log, ct);
            await _logRepository.SaveAsync(ct);
        }

        #region Helpers

        private async Task<User> RequireUserAsync(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            var user = await _userManager.FindByIdAsync(userId);
            return user ?? throw new KeyNotFoundException("User tidak ditemukan.");
        }

        private async Task<string?> BuildAvatarUrlAsync(User user, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(user.AvatarObjectKey))
                return null;

            return await _objectStorage.GetPresignedUrlAsync(
                _storageOptions.Bucket,
                user.AvatarObjectKey,
                TimeSpan.FromMinutes(Math.Max(15, _storageOptions.PresignExpirySeconds / 60)),
                ct
            );
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "avatar.png";

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
            {
                builder.Append(invalid.Contains(c) ? '_' : c);
            }

            return builder.ToString();
        }

        private async Task SafeDeleteAsync(string objectKey)
        {
            try
            {
                await _objectStorage.DeleteAsync(_storageOptions.Bucket, objectKey);
            }
            catch (Exception ex) { }
        }

        private async Task ResetPasswordInternalAsync(
            User user,
            string newPassword,
            string description,
            CancellationToken ct
        )
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await _userManager.ResetPasswordAsync(user, token, newPassword);
            EnsureSucceeded(reset, "Gagal memperbarui password.");
            user.PasswordChangedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await LogEventAsync(
                user.Id,
                SecurityLogEventType.PasswordChanged,
                true,
                description,
                null,
                null,
                ct
            );
        }

        private async Task<string> GetAuthenticatorKeyAsync(User user)
        {
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (!string.IsNullOrWhiteSpace(key))
                return FormatKey(key);

            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
            return FormatKey(key);
        }

        private static IReadOnlyList<string>? ParseRecoveryCodes(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return null;

            return payload
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        private static string FormatKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            var result = new StringBuilder();
            var current = 0;

            foreach (var c in key.ToUpperInvariant())
            {
                if (current == 4)
                {
                    result.Append(' ');
                    current = 0;
                }

                result.Append(c);
                current++;
            }

            return result.ToString();
        }

        private static string? GenerateQrCodeUri(string email, string sharedKey)
        {
            if (string.IsNullOrWhiteSpace(sharedKey))
                return null;

            var issuer = Uri.EscapeDataString("ProcurementHTE");
            return $"otpauth://totp/{issuer}:{Uri.EscapeDataString(email)}?secret={sharedKey.Replace(" ", string.Empty)}&issuer={issuer}&digits=6";
        }

        private static string NormalizeRecoveryCode(string code) =>
            string.IsNullOrWhiteSpace(code)
                ? string.Empty
                : code.Replace(" ", string.Empty, StringComparison.Ordinal);

        private static string? BuildQrImage(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return null;

            var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(7);
            return Convert.ToBase64String(bytes);
        }

        private static string EncodeToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private static string DecodeToken(string encodedToken)
        {
            var bytes = WebEncoders.Base64UrlDecode(encodedToken);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void EnsureSucceeded(IdentityResult result, string message)
        {
            if (result.Succeeded)
                return;

            var builder = new StringBuilder(message);
            foreach (var error in result.Errors)
            {
                builder.Append(' ').Append(error.Description);
            }

            throw new InvalidOperationException(builder.ToString());
        }

        #endregion
    }
}
