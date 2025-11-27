using System.IO;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IObjectStorage
    {
        Task UploadAsync(
            string bucket,
            string objectKey,
            Stream content,
            long size,
            string contentType,
            CancellationToken ct = default
        );

        Task<string> GetPresignedUrlAsync(
            string bucket,
            string objectKey,
            TimeSpan expiry,
            CancellationToken ct = default
        );

        Task<string> GetPresignedUrlHeaderAsync(
            string bucket,
            string objectKey,
            TimeSpan expiry,
            IDictionary<string, string>? responseHeaders,
            CancellationToken ct = default
        );

        Task DeleteAsync(string bucket, string objectKey, CancellationToken ct = default);
    }
}
