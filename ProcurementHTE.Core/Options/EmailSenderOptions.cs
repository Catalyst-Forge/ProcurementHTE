namespace ProcurementHTE.Core.Options
{
    public sealed class EmailSenderOptions
    {
        public bool UseDevelopmentMode { get; set; } = true;
        public string Provider { get; set; } = "Smtp";
        public string FromName { get; set; } = "Procurement HTE";
        public string FromAddress { get; set; } = "noreply@procurementhte.local";

        // Resend API settings
        public string? ApiKey { get; set; }

        // Production SMTP settings
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
