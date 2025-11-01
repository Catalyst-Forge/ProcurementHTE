namespace ProcurementHTE.Core.Models.DTOs
{
    public class RecentActivityDto
    {
        public DateTime Time { get; set; }
        public string? User { get; set; }
        public string? Description { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
