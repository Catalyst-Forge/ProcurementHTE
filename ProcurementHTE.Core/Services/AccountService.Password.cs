using System.Text;
using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Services;

public partial class AccountService
{
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

        user.PasswordChangedAt = _timeProvider.GetUtcNow().UtcDateTime;
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

        await _emailSender.SendAsync(user.Email, "Kode Reset Password Procurement HTE", body, ct);
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
            throw new InvalidOperationException("Kode email tidak valid atau sudah kadaluarsa.");

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

        await _smsSender.SendAsync(user.PhoneNumber, $"Kode reset password Procurement HTE: {code}", ct);
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
}
