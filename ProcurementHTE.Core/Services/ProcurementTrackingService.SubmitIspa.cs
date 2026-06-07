using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    public async Task<ProcurementTrackingResponse> SubmitIspaAsync(
        string procurementId,
        string ispaNumber,
        string submittedByUserId,
        DateTime ispaDate,
        DateTime ispaSubmitDate,
        string ispaFileName,
        string ispaContentType,
        long ispaFileSize,
        Stream fileStream,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        if (procurement.ProcurementStatus != ProcurementStatus.OnSubmitISPA)
        {
            return Failed(
                $"Status procurement saat ini adalah {GetProcurementStatusDescription(procurement.ProcurementStatus)}. ISPA hanya bisa disubmit saat status 'On Submit ISPA'."
            );
        }

        var objectKey = $"procurements/{procurementId}/ispa/{Guid.NewGuid():N}-{ispaFileName}";
        try
        {
            await _objectStorage.UploadAsync(
                _storageOptions.Bucket,
                objectKey,
                fileStream,
                ispaFileSize,
                ispaContentType,
                ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upload ISPA file for procurement {ProcurementId}",
                procurementId
            );
            return Failed("Gagal mengupload file ISPA. Silakan coba lagi.");
        }

        procurement.IspaNumber = ispaNumber;
        procurement.IspaDate = ispaDate;
        procurement.IspaSubmitDate = ispaSubmitDate;
        procurement.IspaSubmittedAt = DateTime.UtcNow;
        procurement.IspaSubmittedByUserId = submittedByUserId;
        procurement.IspaFileName = ispaFileName;
        procurement.IspaFileObjectKey = objectKey;
        procurement.IspaFileContentType = ispaContentType;
        procurement.IspaFileSize = ispaFileSize;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepo.UpdateProcurementAsync(procurement);
        await UpdateProcurementStatusAsync(
            procurementId,
            ProcurementStatus.OnSubmitHardcopy,
            submittedByUserId,
            $"ISPA Number: {ispaNumber}, Tanggal ISPA: {ispaDate:dd MMM yyyy}, Tanggal Submit: {ispaSubmitDate:dd MMM yyyy}",
            ct
        );

        return new ProcurementTrackingResponse
        {
            Success = true,
            Message = "ISPA berhasil disubmit.",
            Data = await GetTrackingByProcurementIdAsync(procurementId, ct),
        };
    }

    private static ProcurementTrackingResponse Failed(string message)
    {
        return new ProcurementTrackingResponse { Success = false, Message = message };
    }
}
