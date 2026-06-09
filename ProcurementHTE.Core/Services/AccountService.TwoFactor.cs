using System.Text;
using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class AccountService
{
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
        ValidateTwoFactorContact(user.Email, user.EmailConfirmed, method, TwoFactorMethod.Email);
        ValidateTwoFactorContact(user.PhoneNumber, user.PhoneNumberConfirmed, method, TwoFactorMethod.Sms);

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
            _ => throw new InvalidOperationException("Metode ini tidak membutuhkan kode terpisah."),
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
                ValidateTwoFactorContact(user.Email, user.EmailConfirmed, method, method);
                await SendTwoFactorEmailAsync(user.Email!, code, ct);
                break;
            case TwoFactorMethod.Sms:
                ValidateTwoFactorContact(user.PhoneNumber, user.PhoneNumberConfirmed, method, method);
                await _smsSender.SendAsync(
                    user.PhoneNumber!,
                    $"Kode verifikasi login Procurement HTE: {code}",
                    ct
                );
                break;
            default:
                throw new InvalidOperationException("Metode ini tidak mendukung pengiriman kode.");
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
        ValidateTwoFactorContact(user.Email, user.EmailConfirmed, method, TwoFactorMethod.Email);
        ValidateTwoFactorContact(user.PhoneNumber, user.PhoneNumberConfirmed, method, TwoFactorMethod.Sms);

        var normalized = verificationCode?.Replace(" ", string.Empty);
        var isValid = method switch
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
        user.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
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

    private async Task SendTwoFactorEmailAsync(string email, string code, CancellationToken ct)
    {
        var body = new StringBuilder()
            .AppendLine("<p>Gunakan kode berikut untuk verifikasi login Anda:</p>")
            .AppendLine($"""<p style="font-size:22px;font-weight:bold;letter-spacing:4px;">{code}</p>""")
            .AppendLine("<p>Kode hanya berlaku sementara. Jangan bagikan kepada siapapun.</p>")
            .ToString();

        await _emailSender.SendAsync(email, "Kode Verifikasi Two-Factor Procurement HTE", body, ct);
    }

    private static void ValidateTwoFactorContact(
        string? destination,
        bool isConfirmed,
        TwoFactorMethod selected,
        TwoFactorMethod expected
    )
    {
        if (selected != expected)
            return;

        if (string.IsNullOrWhiteSpace(destination))
            throw new InvalidOperationException(expected == TwoFactorMethod.Email
                ? "Alamat email tidak tersedia."
                : "Nomor HP belum diisi.");

        if (!isConfirmed)
            throw new InvalidOperationException(expected == TwoFactorMethod.Email
                ? "Verifikasi email terlebih dahulu sebelum memakai Email OTP."
                : "Verifikasi nomor HP terlebih dahulu sebelum memakai SMS OTP.");
    }
}
