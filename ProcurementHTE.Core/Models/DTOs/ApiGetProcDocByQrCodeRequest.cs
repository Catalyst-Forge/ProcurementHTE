using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class ApiGetProcDocByQrCodeRequest
    {
        [Required, MaxLength(1024)]
        public string QrText { get; set; } = default!;
    }
}
