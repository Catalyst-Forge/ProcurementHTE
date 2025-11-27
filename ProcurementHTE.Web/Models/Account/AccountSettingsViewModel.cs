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
}
