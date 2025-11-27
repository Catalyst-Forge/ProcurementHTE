namespace ProcurementHTE.Core.Models.DTOs
{
    public class UploadAvatarRequest
    {
        public string UserId { get; set; } = default!;
        public Stream Content { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = "image/png";
        public long Length { get; set; }
    }
}
