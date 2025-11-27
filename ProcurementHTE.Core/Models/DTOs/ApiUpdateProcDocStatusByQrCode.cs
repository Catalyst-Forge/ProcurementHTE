using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class ApiUpdateProcDocStatusByQrCode
    {
        [Required]
        public string QrText { get; set; } = default!;

        [Required]
        public string Status { get; set; } = default!;
        public string? Reason { get; set; }
        public string? ApprovedByUserId { get; set; }
    }
}
