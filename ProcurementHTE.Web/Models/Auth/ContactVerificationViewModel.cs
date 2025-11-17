namespace ProcurementHTE.Web.Models.Auth
{
    public class ContactVerificationViewModel
    {
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool RequiresEmail { get; set; }
        public bool RequiresPhone { get; set; }
        public string? ReturnUrl { get; set; }
        public string? DevMagicLink { get; set; }
        public string? DevPhoneOtp { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
