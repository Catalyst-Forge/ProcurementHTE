using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<(int uploaded, int total)> GetDocumentCountAsync(
        string procurementId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetWithTrackingDataAsync(procurementId, ct);
        if (procurement == null)
            return (0, 0);

        var needsJustifikasiMap = await _prRepo.GetNeedsJustifikasiMapAsync(
            new List<string> { procurementId },
            ct
        );

        var (totalDocs, uploadedDocs) = CountMandatoryDocs(procurement, needsJustifikasiMap);
        return (uploadedDocs, totalDocs);
    }

    private static (int totalDocs, int uploadedDocs) CountMandatoryDocs(
        Procurement procurement,
        Dictionary<string, bool> needsJustifikasiMap
    )
    {
        var needsJustifikasi = needsJustifikasiMap.GetValueOrDefault(
            procurement.ProcurementId,
            false
        );
        var requiredDocTypes =
            procurement.JobType?.JobTypeDocuments?
                .Where(jtd =>
                    jtd.ProcurementCategory == null
                    || jtd.ProcurementCategory == procurement.ProcurementCategory
                )
                .ToList()
            ?? new List<JobTypeDocuments>();

        if (!needsJustifikasi)
            requiredDocTypes = requiredDocTypes.Where(jtd => jtd.DocumentType?.Name != "Justifikasi").ToList();

        var requiredDocTypeIds = requiredDocTypes
            .Select(jtd => jtd.DocumentTypeId)
            .Distinct()
            .ToHashSet();
        var uploadedDocTypeIds =
            procurement.ProcDocuments?
                .Where(pd => !string.IsNullOrEmpty(pd.DocumentTypeId))
                .Select(pd => pd.DocumentTypeId!)
                .ToHashSet()
            ?? new HashSet<string>();

        return (
            requiredDocTypeIds.Count,
            requiredDocTypeIds.Count(dtId => uploadedDocTypeIds.Contains(dtId))
        );
    }

    public async Task<string?> GetHardcopyEvidenceUrlAsync(
        string procurementId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null || string.IsNullOrEmpty(procurement.HardcopyEvidenceFilePath))
            return null;

        return await _objectStorage.GetPresignedUrlAsync(
            _storageOptions.Bucket,
            procurement.HardcopyEvidenceFilePath,
            TimeSpan.FromHours(1),
            ct
        );
    }

    public async Task<string?> GetIspaFileUrlAsync(
        string procurementId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null || string.IsNullOrEmpty(procurement.IspaFileObjectKey))
            return null;

        return await _objectStorage.GetPresignedUrlAsync(
            _storageOptions.Bucket,
            procurement.IspaFileObjectKey,
            TimeSpan.FromHours(1),
            ct
        );
    }
}
