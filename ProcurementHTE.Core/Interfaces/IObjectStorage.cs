using System.IO;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IObjectStorage
    {
        /// <summary>
        /// Upload object ke bucket storage.
        /// </summary>
        /// <param name="bucket">Nama bucket.</param>
        /// <param name="objectKey">Path lengkap objek (folder/key).</param>
        /// <param name="content">Stream data file.</param>
        /// <param name="size">Ukuran file (bytes).</param>
        /// <param name="contentType">MIME type (misal application/pdf).</param>
        /// <param name="ct">Cancellation token (opsional).</param>
        Task UploadAsync(
            string bucket,
            string objectKey,
            Stream content,
            long size,
            string contentType,
            CancellationToken ct = default
        );

        /// <summary>
        /// Mendapatkan URL presigned GET (download) dengan masa berlaku tertentu.
        /// </summary>
        Task<string> GetPresignedUrlAsync(
            string bucket,
            string objectKey,
            TimeSpan expiry,
            CancellationToken ct = default
        );

        /// <summary>
        /// Hapus object dari bucket.
        /// </summary>
        Task DeleteAsync(
            string bucket,
            string objectKey,
            CancellationToken ct = default
        );
    }
}
