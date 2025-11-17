namespace ProcurementHTE.Core.Models.DTOs
{
    public class UpdateProfileRequest
    {
        public string UserId { get; set; } = default!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? PhoneNumber { get; set; }
        public string Email { get; set; } = null!;
        public string UserName { get; set; } = null!;
    }
}
