namespace ProcurementHTE.Web.Models.Account
{
    public class UserSessionViewModel
    {
        public string SessionId { get; set; } = null!;
        public string Device { get; set; } = "Tidak diketahui";
        public string Browser { get; set; } = "Tidak diketahui";
        public string? IpAddress { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrent { get; set; }
    }
}
