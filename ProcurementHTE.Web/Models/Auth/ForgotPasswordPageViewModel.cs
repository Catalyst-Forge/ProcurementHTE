using ProcurementHTE.Web.Models.Auth;

namespace ProcurementHTE.Web.Models.Auth
{
    public class ForgotPasswordPageViewModel
    {
        public ForgotPasswordRecoveryViewModel Recovery { get; set; } = new();
        public ForgotPasswordResetWithCodeViewModel EmailReset { get; set; } = new();
        public ForgotPasswordResetWithCodeViewModel SmsReset { get; set; } = new();
        public string ActiveTab { get; set; } = "recovery";
    }
}
