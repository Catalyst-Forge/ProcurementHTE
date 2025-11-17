using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Models.Enums;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IAccountService
    {
        Task<AccountOverviewDto> GetOverviewAsync(string userId, CancellationToken ct = default);
        Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
        Task<string?> UploadAvatarAsync(UploadAvatarRequest request, CancellationToken ct = default);
        Task RemoveAvatarAsync(string userId, CancellationToken ct = default);
        Task ChangePasswordAsync(
            string userId,
            string currentPassword,
            string newPassword,
            CancellationToken ct = default
        );
        Task<TwoFactorSummaryDto> GetTwoFactorSummaryAsync(string userId, CancellationToken ct = default);
        Task<string> GenerateTwoFactorCodeAsync(
            string userId,
            TwoFactorMethod method,
            CancellationToken ct = default
        );
        Task SendTwoFactorCodeAsync(
            string userId,
            TwoFactorMethod method,
            string code,
            CancellationToken ct = default
        );
        Task EnableTwoFactorAsync(
            string userId,
            TwoFactorMethod method,
            string verificationCode,
            CancellationToken ct = default
        );
        Task DisableTwoFactorAsync(string userId, CancellationToken ct = default);
        Task<IEnumerable<string>> GenerateRecoveryCodesAsync(
            string userId,
            CancellationToken ct = default
        );
        Task<IReadOnlyList<UserSession>> GetSessionsAsync(string userId, CancellationToken ct = default);
        Task<UserSession> RegisterSessionAsync(
            string userId,
            string? userAgent,
            string? ipAddress,
            string? device,
            string? browser,
            string? location,
            bool isCurrent,
            CancellationToken ct = default
        );
        Task DeactivateSessionAsync(string userId, string sessionId, CancellationToken ct = default);
        Task DeactivateAllSessionsAsync(string userId, CancellationToken ct = default);
        Task<IReadOnlyList<UserSecurityLog>> GetSecurityLogsAsync(
            string userId,
            int take,
            CancellationToken ct = default
        );
        Task LogEventAsync(
            string userId,
            SecurityLogEventType eventType,
            bool success,
            string? description,
            string? ipAddress,
            string? userAgent,
            CancellationToken ct = default
        );
        Task<string> GenerateEmailVerificationTokenAsync(string userId, CancellationToken ct = default);
        Task SendEmailVerificationAsync(string userId, string callbackUrl, CancellationToken ct = default);
        Task ConfirmEmailAsync(string userId, string encodedToken, CancellationToken ct = default);
        Task<string> GeneratePhoneVerificationCodeAsync(string userId, CancellationToken ct = default);
        Task SendPhoneVerificationCodeAsync(string userId, string code, CancellationToken ct = default);
        Task ConfirmPhoneAsync(string userId, string code, CancellationToken ct = default);
        Task ResetPasswordWithRecoveryCodeAsync(
            string userId,
            string recoveryCode,
            string newPassword,
            CancellationToken ct = default
        );
        Task<string> GeneratePasswordResetEmailCodeAsync(string userId, CancellationToken ct = default);
        Task SendPasswordResetEmailCodeAsync(string userId, string code, CancellationToken ct = default);
        Task ResetPasswordWithEmailCodeAsync(
            string userId,
            string code,
            string newPassword,
            CancellationToken ct = default
        );
        Task<string> GeneratePasswordResetSmsCodeAsync(string userId, CancellationToken ct = default);
        Task SendPasswordResetSmsCodeAsync(string userId, string code, CancellationToken ct = default);
        Task ResetPasswordWithSmsCodeAsync(
            string userId,
            string code,
            string newPassword,
            CancellationToken ct = default
        );
        Task ResetPasswordForUserAsync(
            string userId,
            string newPassword,
            string description,
            CancellationToken ct = default
        );
        Task<IReadOnlyList<string>?> GetRecoveryCodesSnapshotAsync(
            string userId,
            CancellationToken ct = default
        );
        Task SetRecoveryCodesHiddenAsync(string userId, bool hidden, CancellationToken ct = default);
    }
}
