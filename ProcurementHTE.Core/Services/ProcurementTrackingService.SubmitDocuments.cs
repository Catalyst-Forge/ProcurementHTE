using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<ProcurementTrackingResponse> SubmitJustificationAsync(
        string procurementId,
        string submittedByUserId,
        string hardcopyEvidenceFileName,
        string hardcopyEvidenceContentType,
        long hardcopyEvidenceFileSize,
        Stream fileStream,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        if (procurement.ProcurementStatus != ProcurementStatus.OnSubmitHardcopy)
        {
            return Failed(
                $"Status procurement saat ini adalah {GetProcurementStatusDescription(procurement.ProcurementStatus)}. Hardcopy hanya bisa disubmit saat status 'On Submit Hardcopy'."
            );
        }

        var objectKey =
            $"procurements/{procurementId}/evidence-hardcopy/{Guid.NewGuid():N}-{hardcopyEvidenceFileName}";
        await _objectStorage.UploadAsync(
            _storageOptions.Bucket,
            objectKey,
            fileStream,
            hardcopyEvidenceFileSize,
            hardcopyEvidenceContentType,
            ct
        );

        procurement.HardcopyEvidenceFileName = hardcopyEvidenceFileName;
        procurement.HardcopyEvidenceFilePath = objectKey;
        procurement.HardcopyEvidenceContentType = hardcopyEvidenceContentType;
        procurement.HardcopyEvidenceFileSize = hardcopyEvidenceFileSize;
        procurement.HardcopySubmittedAt = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.HardcopySubmittedByUserId = submittedByUserId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurementId,
            ProcurementStatus.OnSubmitPO,
            submittedByUserId,
            "Hardcopy evidence uploaded",
            ct
        );

        return new ProcurementTrackingResponse
        {
            Success = true,
            Message = "Hardcopy evidence berhasil disubmit.",
            Data = await GetTrackingByProcurementIdAsync(procurementId, ct),
        };
    }

    public async Task<ProcurementTrackingResponse> SubmitPoAsync(
        string procurementId,
        string poNumber,
        string submittedByUserId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        if (procurement.ProcurementStatus != ProcurementStatus.OnSubmitPO)
        {
            return Failed(
                $"Status procurement saat ini adalah {GetProcurementStatusDescription(procurement.ProcurementStatus)}. PO hanya bisa disubmit saat status 'On Submit PO'."
            );
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.PoNumber = poNumber;
        procurement.PoSubmittedAt = now;
        procurement.PoSubmittedByUserId = submittedByUserId;
        procurement.UpdatedAt = now;

        // Done PO ends the procurement, so the lifecycle status must follow;
        // otherwise the details page keeps showing "In Progress".
        var completedStatus = await _procurementRepo.GetStatusByNameAsync("Completed");
        if (completedStatus != null)
        {
            procurement.StatusId = completedStatus.StatusId;
            procurement.CompletedAt = now;
        }
        else
        {
            _logger.LogWarning(
                "Status 'Completed' tidak ditemukan; procurement {ProcurementId} tetap pada status siklus lamanya",
                procurementId
            );
        }

        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurementId,
            ProcurementStatus.DonePO,
            submittedByUserId,
            $"PO Number: {poNumber}",
            ct
        );

        return new ProcurementTrackingResponse
        {
            Success = true,
            Message = "PO berhasil disubmit. Procurement selesai!",
            Data = await GetTrackingByProcurementIdAsync(procurementId, ct),
        };
    }
}
