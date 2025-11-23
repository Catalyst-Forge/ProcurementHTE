namespace ProcurementHTE.Web.Models {
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public int StatusCode { get; set; } = 500;
        public string Title { get; set; } = "Terjadi Kesalahan";
        public string Description { get; set; } = "Permintaan Anda tidak dapat kami proses saat ini.";
        public string? RequestPath { get; set; }
        public string PrimaryActionText { get; set; } = "Kembali ke Dashboard";
        public string PrimaryActionUrl { get; set; } = "/";
        public string? SecondaryActionText { get; set; }
        public string? SecondaryActionUrl { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public bool ShowRequestPath => !string.IsNullOrWhiteSpace(RequestPath);
        public bool ShowSecondaryAction => !string.IsNullOrWhiteSpace(SecondaryActionText) && !string.IsNullOrWhiteSpace(SecondaryActionUrl);
    }
}
