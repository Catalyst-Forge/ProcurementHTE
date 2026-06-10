using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public sealed partial class ProcDocumentService
{
    public async Task<bool> CanSendApprovalAsync(
        string procurementId,
        CancellationToken ct = default
    )
    {
        var procurement = await GetProcurementOrThrowAsync(procurementId);
        if (string.IsNullOrWhiteSpace(procurement.JobTypeId))
            return false;

        var jobTypeDocs = await _jobTypeDocumentRepository.ListByJobTypeAsync(
            procurement.JobTypeId,
            procurement.ProcurementCategory,
            ct
        );

        var docs = await _procDocumentRepository.GetByProcurementAsync(procurementId);
        var availableDocTypes = docs
            .Select(d => d.DocumentTypeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (jobTypeDocs.Count == 0)
            return availableDocTypes.Count > 0;

        var required = jobTypeDocs
            .Where(d => d.IsMandatory || d.IsUploadRequired || d.RequiresApproval)
            .ToList();

        return required.Count == 0
            ? availableDocTypes.Count > 0
            : required.All(cfg => availableDocTypes.Contains(cfg.DocumentTypeId));
    }

    public async Task SendApprovalAsync(
        string procDocumentId,
        string requestedByUserId,
        CancellationToken ct = default
    )
    {
        var doc = await _procDocumentRepository.GetByIdAsync(procDocumentId);
        if (doc == null)
            throw new InvalidOperationException("Dokumen tidak ditemukan.");

        if (await IsRoundLetterDocumentAsync(doc))
            return;

        var procurement = await GetProcurementOrThrowAsync(doc.ProcurementId);
        _ = string.IsNullOrWhiteSpace(procurement.JobTypeId)
            ? Array.Empty<JobTypeDocuments>()
            : await _jobTypeDocumentRepository.ListByJobTypeAsync(
                procurement.JobTypeId,
                procurement.ProcurementCategory,
                ct
            );
    }

    public Task<string?> GetPresignedQrUrlAsync(
        string procDocumentId,
        TimeSpan expiry,
        CancellationToken ct = default
    )
    {
        return Task.FromResult<string?>(null);
    }

    private async Task<bool> IsRoundLetterDocumentAsync(ProcDocuments doc)
    {
        try
        {
            var docType = await _documentTypeRepository.GetByIdAsync(doc.DocumentTypeId);
            var name = docType?.Name ?? doc.DocumentType?.Name ?? string.Empty;
            var fileName = doc.FileName ?? string.Empty;

            return name.Equals("Surat Penawaran Harga", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Surat Negosiasi Harga", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("SPH_", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("SNH_", StringComparison.OrdinalIgnoreCase)
                || (fileName.Contains("Penawaran", StringComparison.OrdinalIgnoreCase)
                    && fileName.Contains("Harga", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve document type for {ProcDocumentId}.", doc.ProcDocumentId);
            return false;
        }
    }
}
