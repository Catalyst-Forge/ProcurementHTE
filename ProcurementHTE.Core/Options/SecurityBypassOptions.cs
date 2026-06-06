namespace ProcurementHTE.Core.Options {
    public class SecurityBypassOptions
    {
        public bool BypassContactVerification { get; set; }
        public bool BypassPhoneVerification { get; set; }
        public bool BypassTwoFactor { get; set; }
    }
}
