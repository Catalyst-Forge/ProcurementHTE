using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementTrackingService
{
    private const string CancelledStatusName = "Cancelled";

    public async Task<ProcurementTrackingResponse> CancelProcurementAsync(
        string procurementId,
        string cancelledByUserId,
        CancellationToken ct = default
    )
    {
        var procurement = await _procurementRepo.GetByIdAsync(procurementId);
        if (procurement == null)
            return Failed("Procurement tidak ditemukan.");

        if (procurement.ProcurementStatus == ProcurementStatus.Cancelled)
            return Failed("Procurement ini sudah di-cancel.");

        if (
            procurement.ProcurementStatus == ProcurementStatus.DonePO
            || procurement.Status?.StatusName is "Completed" or "Closed"
        )
            return Failed("Procurement yang sudah selesai tidak bisa di-cancel.");

        var cancelledStatus = await _procurementRepo.GetStatusByNameAsync(CancelledStatusName);
        if (cancelledStatus == null)
            return Failed("Status 'Cancelled' belum tersedia.");

        procurement.StatusId = cancelledStatus.StatusId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepo.UpdateProcurementAsync(procurement);

        await UpdateProcurementStatusAsync(
            procurementId,
            ProcurementStatus.Cancelled,
            cancelledByUserId,
            "Cancelled",
            ct
        );

        await NotifyApPoUsersAsync(
            title: $"Procurement {procurement.ProcNum} di-cancel",
            message: $"WO: {procurement.Wonum}",
            notificationType: "ProcurementCancelled",
            actionUrl: $"/Procurements/Details/{procurementId}",
            referenceId: procurementId,
            createdByUserId: cancelledByUserId,
            ct: ct
        );

        return Succeeded("Procurement berhasil di-cancel.");
    }
}
