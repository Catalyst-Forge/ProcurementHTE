using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class ApiResolveQrRequest
    {
        [Required, MaxLength(1024)]
        public string QrText { get; set; } = default!;

        // opsional: hanya ambil yang Pending
        public bool OnlyPending { get; set; } = true;
    }
}
