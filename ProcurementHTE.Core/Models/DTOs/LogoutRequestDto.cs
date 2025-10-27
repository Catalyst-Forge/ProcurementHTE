namespace ProcurementHTE.Core.Models.DTOs
{
    public class LogoutRequestDto
    {
        // Jika RevokeAllForDevice = true, RefreshToken boleh kosong
        public string? RefreshToken { get; set; }
        public string? DeviceId { get; set; }
        public bool RevokeAllForDevice { get; set; } = false;
    }
}
