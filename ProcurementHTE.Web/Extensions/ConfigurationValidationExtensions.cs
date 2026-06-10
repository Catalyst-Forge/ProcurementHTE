using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ProcurementHTE.Web.Extensions;

public static class ConfigurationValidationExtensions
{
    private const string DefaultConnectionString =
        "Server=.;Database=ProcurementHTE;Trusted_Connection=True;MultipleActiveResultSets=true";

    public static void ValidateProductionSettings(
        this IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        if (!environment.IsProduction())
            return;

        var failures = new List<string>();
        var jwtSection = configuration.GetJwtSettingsSection();
        var jwtSecretKey = $"{jwtSection.Path}:Secret";

        RequireNotPlaceholder(
            failures,
            configuration.GetConnectionString("DefaultConnection"),
            "ConnectionStrings:DefaultConnection",
            DefaultConnectionString
        );

        RequireNotPlaceholder(
            failures,
            jwtSection["Secret"],
            jwtSecretKey,
            "CHANGE_THIS_IN_PRODUCTION"
        );

        RequireMinimumLength(failures, jwtSection["Secret"], jwtSecretKey, 32);

        RequireNotPlaceholder(
            failures,
            configuration["ObjectStorage:AccessKey"],
            "ObjectStorage:AccessKey"
        );
        RequireNotPlaceholder(
            failures,
            configuration["ObjectStorage:SecretKey"],
            "ObjectStorage:SecretKey"
        );
        RequireNotPlaceholder(failures, configuration["ObjectStorage:Bucket"], "ObjectStorage:Bucket");

        ValidateEmailSettings(configuration, failures);
        ValidateSmsSettings(configuration, failures);

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Production configuration is invalid: " + string.Join("; ", failures)
            );
        }
    }

    private static void ValidateEmailSettings(IConfiguration configuration, List<string> failures)
    {
        var useDevelopmentMode = configuration.GetValue<bool>("EmailSender:UseDevelopmentMode");
        if (useDevelopmentMode)
        {
            failures.Add("EmailSender:UseDevelopmentMode must be false in production");
            return;
        }

        var provider = configuration["EmailSender:Provider"];
        if (string.Equals(provider, "Resend", StringComparison.OrdinalIgnoreCase))
        {
            RequireNotPlaceholder(failures, configuration["EmailSender:ApiKey"], "EmailSender:ApiKey");
            return;
        }

        RequireNotPlaceholder(
            failures,
            configuration["EmailSender:SmtpHost"],
            "EmailSender:SmtpHost",
            "smtp.yourprovider.com"
        );
        RequireNotPlaceholder(
            failures,
            configuration["EmailSender:Username"],
            "EmailSender:Username",
            "smtp-user"
        );
        RequireNotPlaceholder(
            failures,
            configuration["EmailSender:Password"],
            "EmailSender:Password",
            "smtp-password"
        );
    }

    private static void ValidateSmsSettings(IConfiguration configuration, List<string> failures)
    {
        var useDevelopmentMode = configuration.GetValue<bool>("SmsSender:UseDevelopmentMode");
        if (useDevelopmentMode)
        {
            failures.Add("SmsSender:UseDevelopmentMode must be false in production");
            return;
        }

        var bypassContactVerification = configuration.GetValue<bool>(
            "SecurityBypass:BypassContactVerification"
        );
        var bypassPhoneVerification = configuration.GetValue<bool>(
            "SecurityBypass:BypassPhoneVerification"
        );
        if (bypassContactVerification || bypassPhoneVerification)
            return;

        RequireNotPlaceholder(failures, configuration["SmsSender:ProviderUrl"], "SmsSender:ProviderUrl");
        RequireNotPlaceholder(failures, configuration["SmsSender:ApiKey"], "SmsSender:ApiKey");
    }

    private static void RequireNotPlaceholder(
        List<string> failures,
        string? value,
        string key,
        string? placeholder = null
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required");
            return;
        }

        if (!string.IsNullOrWhiteSpace(placeholder)
            && string.Equals(value.Trim(), placeholder, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{key} still uses the default placeholder");
        }
    }

    private static void RequireMinimumLength(
        List<string> failures,
        string? value,
        string key,
        int minLength
    )
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Length < minLength)
        {
            failures.Add($"{key} must be at least {minLength} characters");
        }
    }
}
