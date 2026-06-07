using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Extensions;

internal static class JwtSettingsConfiguration
{
    private const string PreferredSectionName = "JwtSettings";
    private const string LegacySectionName = "Jwt";

    public static IConfigurationSection GetJwtSettingsSection(this IConfiguration configuration)
    {
        var preferred = configuration.GetSection(PreferredSectionName);
        if (preferred.Exists())
            return preferred;

        var legacy = configuration.GetSection(LegacySectionName);
        return legacy.Exists() ? legacy : preferred;
    }

    public static JwtSettings GetRequiredJwtSettings(this IConfiguration configuration)
    {
        var section = configuration.GetJwtSettingsSection();
        var settings = section.Get<JwtSettings>();

        if (settings is null)
        {
            throw new InvalidOperationException($"{section.Path} section is missing.");
        }

        RequireValue(settings.Secret, $"{section.Path}:Secret");
        RequireValue(settings.Issuer, $"{section.Path}:Issuer");
        RequireValue(settings.Audience, $"{section.Path}:Audience");

        if (settings.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                $"{section.Path}:Secret must be at least 32 characters long."
            );
        }

        return settings;
    }

    private static void RequireValue(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is not configured.");
        }
    }
}
