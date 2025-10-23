using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Options;
using ProcurementHTE.Infrastructure.Storage;

namespace ProcurementHTE.Infrastructure.Storage
{
    public class MinioStorage : IObjectStorage
    {
        private readonly IMinioClient _client;   // ⬅️ IMinioClient (bukan MinioClient)
        private readonly ObjectStorageOptions _options;

        public MinioStorage(IOptions<ObjectStorageOptions> options)
        {
            _options = options.Value;

            Console.WriteLine($"[MinIO cfg] Endpoint={_options.Endpoint}, SSL={_options.UseSSL}, Bucket={_options.Bucket}, AccessKey={_options.AccessKey}");

            _client = new MinioClient()
                .WithEndpoint(_options.Endpoint)
                .WithCredentials(_options.AccessKey, _options.SecretKey)
                .WithSSL(_options.UseSSL)
                .Build(); 
        }

        public Task DeleteAsync(string bucket, string objectKey, CancellationToken ct = default) =>
            _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(bucket).WithObject(objectKey), ct);

        public async Task UploadAsync(string bucket, string objectKey, Stream content, long size, string contentType, CancellationToken ct = default)
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


        public Task<string> GetPresignedUrlAsync(string bucket, string objectKey, TimeSpan expiry, CancellationToken ct = default) =>
            _client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithExpiry((int)expiry.TotalSeconds));
    }
}
