using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Infrastructure.Storage
{
    public class MinioStorage : IObjectStorage
    {
        private readonly IMinioClient _client;
        private readonly ObjectStorageOptions _options;

        public MinioStorage(IOptions<ObjectStorageOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_options.Endpoint))
                throw new ArgumentException("MinIO endpoint is required (Minio:Endpoint).");

            if (string.IsNullOrWhiteSpace(_options.AccessKey))
                throw new ArgumentException("MinIO access key is required (Minio:AccessKey).");

            if (string.IsNullOrWhiteSpace(_options.SecretKey))
                throw new ArgumentException("MinIO secret key is required (Minio:SecretKey).");

            // --- Sanitize endpoint: buang skema & trailing slash ---
            var endpoint = _options.Endpoint.Trim().TrimEnd('/');
            var schemeIdx = endpoint.IndexOf("://", StringComparison.Ordinal);
            if (schemeIdx >= 0)
                endpoint = endpoint[(schemeIdx + 3)..]; // potong "http(s)://"

            var client = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(_options.AccessKey, _options.SecretKey);

            if (_options.UseSSL)
                client = client.WithSSL(); // aktifkan https

            _client = client.Build();
        }

        public Task DeleteAsync(string bucket, string objectKey, CancellationToken ct = default) =>
            _client.RemoveObjectAsync(
                new RemoveObjectArgs().WithBucket(bucket).WithObject(objectKey),
                ct
            );

        public async Task UploadAsync(
            string bucket,
            string objectKey,
            Stream content,
            long size,
            string contentType,
            CancellationToken ct = default
        )
        {
            await _client.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey)
                    .WithStreamData(content)
                    .WithObjectSize(size)
                    .WithContentType(contentType),
                ct
            );
        }

        public Task<string> GetPresignedUrlAsync(
            string bucket,
            string objectKey,
            TimeSpan expiry,
            CancellationToken ct = default
        ) =>
            _client.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey)
                    .WithExpiry((int)expiry.TotalSeconds)
            );

        public async Task<string> GetPresignedUrlHeaderAsync(
            string bucket,
            string objectKey,
            TimeSpan expiry,
            IDictionary<string, string>? responseHeaders,
            CancellationToken ct = default
        )
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithExpiry((int)expiry.TotalSeconds);

            if (responseHeaders is not null && responseHeaders.Count > 0)
                args = args.WithHeaders(responseHeaders);

            // Tetap tanpa ct
            return await _client.PresignedGetObjectAsync(args).ConfigureAwait(false);
        }
    }
}
