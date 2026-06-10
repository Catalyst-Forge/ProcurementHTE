using System.Text;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public sealed partial class ProcDocumentService
{
    private async Task<Procurement> GetProcurementOrThrowAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ProcurementId tidak boleh kosong.", nameof(procurementId));

        var procurement = await _procurementRepository.GetByIdAsync(procurementId);
        return procurement
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID '{procurementId}' tidak ditemukan."
            );
    }

    private TimeSpan GetExpiry(TimeSpan? overrideValue)
    {
        if (overrideValue.HasValue && overrideValue.Value > TimeSpan.Zero)
            return overrideValue.Value;

        var seconds = Math.Max(60, _storageOptions.PresignExpirySeconds);
        return TimeSpan.FromSeconds(seconds);
    }

    private static string BuildDocumentObjectKey(
        string procurementId,
        string documentTypeId,
        string fileName
    )
    {
        var sanitized = SanitizeFileName(fileName);
        return $"procurements/{procurementId}/documents/{documentTypeId}/{Guid.NewGuid():N}-{sanitized}";
    }

    private static string BuildGeneratedObjectKey(
        string procurementId,
        string documentTypeId,
        string fileName
    ) =>
        $"procurements/{procurementId}/generated/{documentTypeId}/{Guid.NewGuid():N}-{SanitizeFileName(fileName)}";

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file.dat";

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var ch in fileName.Trim())
        {
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        }

        return builder.ToString();
    }

    private async Task SafeDeleteAsync(string objectKey)
    {
        try
        {
            await _objectStorage.DeleteAsync(_storageOptions.Bucket, objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete object storage file {ObjectKey} from bucket {Bucket}.",
                objectKey,
                _storageOptions.Bucket
            );
        }
    }
}
