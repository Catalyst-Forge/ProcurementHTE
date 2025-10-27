namespace ProcurementHTE.Core.Models.DTOs {
    public class LogoutResponseDto {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
        public string? Mode { get; set; } // "single" | "device-all"
        public string? DeviceId { get; set; }
        public int RevokedTokens { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
