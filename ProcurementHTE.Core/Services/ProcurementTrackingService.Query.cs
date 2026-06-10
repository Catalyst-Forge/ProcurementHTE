using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<ProcurementTrackingDto?> GetTrackingByProcurementIdAsync(
        string procurementId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetWithTrackingDataAsync(procurementId, ct);
        if (procurement == null)
            return null;

        var needsJustifikasi = await _prRepo.GetNeedsJustifikasiMapAsync(
            new List<string> { procurementId },
            ct
        );

        return MapToDto(procurement, needsJustifikasi);
    }

    public async Task<ProcurementTrackingDto?> GetTrackingByProcNumAsync(
        string procNum,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByProcNumWithTrackingAsync(procNum, ct);
        if (procurement == null)
            return null;

        var needsJustifikasi = await _prRepo.GetNeedsJustifikasiMapAsync(
            new List<string> { procurement.ProcurementId },
            ct
        );

        return MapToDto(procurement, needsJustifikasi);
    }

    public async Task<PRWithProcurementsTrackingDto?> GetPrWithProcurementsTrackingAsync(
        string prId,
        CancellationToken ct = default
    )
    {
        var pr = await _prRepo.GetWithTrackingIncludesByPrIdAsync(prId, ct);
        if (pr == null)
            return null;

        var procurements = await _procurementRepo.GetByPrIdWithTrackingAsync(prId, ct);
        var procIds = procurements.Select(p => p.ProcurementId).ToList();
        var needsJustifikasiMap = await _prRepo.GetNeedsJustifikasiMapAsync(procIds, ct);

        return new PRWithProcurementsTrackingDto
        {
            PrId = pr.PrId,
            PrNumber = pr.PrNumber,
            RequestDate = pr.RequestDate,
            Description = pr.Description,
            CurrentStatus = pr.DerivedStatus,
            CurrentStatusDescription = GetStatusDescription(pr.DerivedStatus),
            Procurements = procurements.Select(p => MapToDto(p, needsJustifikasiMap)).ToList(),
        };
    }
}
