using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using QRCoder;

namespace ProcurementHTE.Core.Services;

public partial class AccountService
{
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
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete account storage file {ObjectKey} from bucket {Bucket}.",
                objectKey,
                _storageOptions.Bucket
            );
        }
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
        user.PasswordChangedAt = _timeProvider.GetUtcNow().UtcDateTime;
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
}
