using ProcurementHTE.Core.Models.Enums;

namespace ProcurementHTE.Web.Models.Account
{
    public class TwoFactorSettingsViewModel
    {
        public bool IsEnabled { get; set; }
        public TwoFactorMethod SelectedMethod { get; set; }
        public string? SharedKey { get; set; }
        public string? AuthenticatorUri { get; set; }
        public string? AuthenticatorQrBase64 { get; set; }
        public int RecoveryCodesLeft { get; set; }
        public IEnumerable<string>? NewlyGeneratedRecoveryCodes { get; set; }
    }
}
