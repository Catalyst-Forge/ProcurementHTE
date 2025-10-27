using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public class RefreshRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = default!;
        public string? DeviceId { get; set; }
    }
}
