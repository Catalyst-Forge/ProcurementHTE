using ProcurementHTE.Core.Models.Enums;

namespace ProcurementHTE.Web.Models.Auth
{
    public class TwoFactorSetupViewModel
    {
        public bool EmailAvailable { get; set; }
        public bool PhoneAvailable { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? SharedKey { get; set; }
        public string? AuthenticatorUri { get; set; }
        public string? AuthenticatorQrBase64 { get; set; }
        public string? ReturnUrl { get; set; }
        public TwoFactorMethod SelectedMethod { get; set; } = TwoFactorMethod.AuthenticatorApp;
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ActiveTab { get; set; }
    }
}
