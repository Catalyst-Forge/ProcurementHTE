namespace ProcurementHTE.Core.Options
{
    public sealed class SmsSenderOptions
    {
        public bool UseDevelopmentMode { get; set; } = true;
        public string SenderName { get; set; } = "ProcHTE";

        // Production HTTP provider settings
        public string? ProviderUrl { get; set; }
        public string? ApiKey { get; set; }
    }
}
