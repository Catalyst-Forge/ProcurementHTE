using System.Text;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Services;

public partial class AccountService
{
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
            throw new ArgumentException("Callback url tidak boleh kosong.", nameof(callbackUrl));

        var user = await RequireUserAsync(userId);
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException("Email pengguna belum diisi.");

        var body = new StringBuilder()
            .AppendLine("<p>Halo,</p>")
            .AppendLine("<p>Klik tautan berikut untuk memverifikasi email Anda:</p>")
            .AppendLine($"""<p><a href="{callbackUrl}" style="font-weight:600;">Verifikasi Sekarang</a></p>""")
            .AppendLine("<p>Abaikan jika Anda tidak meminta verifikasi ini.</p>")
            .ToString();

        await _emailSender.SendAsync(user.Email, "Verifikasi Email Procurement HTE", body, ct);
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
}
