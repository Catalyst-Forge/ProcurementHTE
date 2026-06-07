using Microsoft.AspNetCore.Http;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Constants;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AccountController
{
    private Task<User?> GetCurrentUserAsync() => _userManager.GetUserAsync(User);

    private string? GetCurrentSessionId() => Request.Cookies[CookieNames.Session];

    private string? GetRemoteIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private bool CanUseSmsTwoFactor(User user) =>
        !_securityBypassOptions.BypassPhoneVerification
        && IsSmsVerificationAvailable()
        && !string.IsNullOrWhiteSpace(user.PhoneNumber)
        && user.PhoneNumberConfirmed;

    private bool IsSmsVerificationAvailable() =>
        _smsOptions.UseDevelopmentMode
        || (
            !string.IsNullOrWhiteSpace(_smsOptions.ProviderUrl)
            && !string.IsNullOrWhiteSpace(_smsOptions.ApiKey)
        );

    private bool IsCooldownActive(string key, out int remainingSeconds)
    {
        remainingSeconds = CooldownHelper.GetRemainingSeconds(HttpContext.Session, key);
        return remainingSeconds > 0;
    }

    private void StartCooldown(string key) =>
        CooldownHelper.SetCooldown(
            HttpContext.Session,
            key,
            TimeSpan.FromSeconds(VerificationCooldownSeconds)
        );

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

    private static (byte[] Buffer, string ContentType, string FileName) ParseImagePayload(
        string base64
    )
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

        var contentType =
            metadata
                .ToString()
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(part =>
                    part.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                )
                ?.Replace("data:", string.Empty, StringComparison.OrdinalIgnoreCase)
            ?? "image/png";

        var buffer = Convert.FromBase64String(payload.ToString());
        var extension = contentType switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            _ => "png",
        };

        return (buffer, contentType, $"avatar.{extension}");
    }
}
