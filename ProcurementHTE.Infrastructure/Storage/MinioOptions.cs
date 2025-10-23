using System.ComponentModel.DataAnnotations;

namespace ProcurementHTE.Infrastructure.Storage;

public sealed class MinioOptions
{
    [Required] public string Endpoint { get; set; } = default!; // contoh: "localhost:9000" (tanpa http)
    [Required] public string AccessKey { get; set; } = default!;
    [Required] public string SecretKey { get; set; } = default!;
    public bool UseSSL { get; set; } = false;
    [Required] public string Bucket { get; set; } = default!;
}
