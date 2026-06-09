using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.Account
{
    public class AccountSettingsViewModel
    {
        public AccountOverviewDto Overview { get; set; } = null!;
        public UpdateProfileInputModel Profile { get; set; } = new();
        public ChangePasswordInputModel ChangePassword { get; set; } = new();
        public TwoFactorSettingsViewModel TwoFactor { get; set; } = new();
        public IReadOnlyList<UserSessionViewModel> Sessions { get; set; } =
            Array.Empty<UserSessionViewModel>();
        public IReadOnlyList<UserSecurityLog> SecurityLogs { get; set; } =
            Array.Empty<UserSecurityLog>();
    }

    public class AccountSettingsPageViewModel
    {
        public AccountSettingsViewModel Settings { get; init; } = default!;
        public bool ShowPhoneVerify { get; init; }
        public string? DevMagicLink { get; init; }
        public string? DevPhoneOtp { get; init; }
        public int EmailVerificationCooldown { get; init; }
        public int PhoneVerificationCooldown { get; init; }
        public bool SmsVerificationAvailable { get; init; }
        public bool RequireEmailVerificationForTwoFactor { get; init; }
        public bool RequirePhoneVerificationForTwoFactor { get; init; }

        public string AvatarUrl => Settings.Overview.AvatarUrl
            ?? "/img/default-avatar.svg";
        public string FullName => string.Join(" ", new[] { Settings.Overview.FirstName, Settings.Overview.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        public string Roles => Settings.Overview.Roles?.Count > 0 ? string.Join(", ", Settings.Overview.Roles) : "Role belum diatur";
        public string JobTitle => string.IsNullOrWhiteSpace(Settings.Overview.JobTitle) ? "Belum diatur" : Settings.Overview.JobTitle;
        public string LastLogin => Settings.Overview.LastLoginAt?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "Belum pernah";
        public bool ContactVerificationComplete => Settings.Overview.EmailConfirmed
            && (Settings.Overview.PhoneNumberConfirmed || !SmsVerificationAvailable);
        public bool RecoveryCodesHidden => Settings.Overview.RecoveryCodesHidden;
        public bool HasStoredRecoveryCodes => Settings.TwoFactor.NewlyGeneratedRecoveryCodes?.Any() == true;
    }
}
