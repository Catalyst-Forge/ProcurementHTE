namespace ProcurementHTE.Core.Models.DTOs
{
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshExpiresAt { get; set; }
        public UserDataDto? User { get; set; }
    }
}
