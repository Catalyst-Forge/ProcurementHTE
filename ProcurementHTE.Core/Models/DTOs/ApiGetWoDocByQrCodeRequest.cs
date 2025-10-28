using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcurementHTE.Core.Models.DTOs
{
    public sealed class ApiGetWoDocByQrCodeRequest
    {
        [Required, MaxLength(1024)] public string QrText { get; set; } = default!;
    }
}
