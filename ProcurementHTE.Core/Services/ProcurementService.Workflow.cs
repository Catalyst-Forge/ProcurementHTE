using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementService
{
    public async Task MarkAsCompletedAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement =
            await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );

        var completedStatus = await GetCompletedStatusAsync();
        procurement.StatusId = completedStatus.StatusId;
        procurement.CompletedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task ApproveByAppoAsync(string procurementId, string appoUserId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        if (string.IsNullOrWhiteSpace(appoUserId))
            throw new ArgumentException("User ID AP-PO tidak boleh kosong", nameof(appoUserId));

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        if (procurement.Status?.StatusName != "In Progress")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'In Progress' yang dapat diapprove"
            );

        if (!string.IsNullOrWhiteSpace(procurement.AppoUserId))
            throw new InvalidOperationException("Procurement ini sudah di-approve oleh AP-PO");

        procurement.AppoUserId = appoUserId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task RejectByAppoAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        if (procurement.Status?.StatusName != "In Progress")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'In Progress' yang dapat direject"
            );

        var createdStatus =
            await _procurementRepository.GetStatusByNameAsync("Created")
            ?? throw new InvalidOperationException("Status 'Created' tidak ditemukan");

        procurement.StatusId = createdStatus.StatusId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task PublishAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        if (procurement.Status?.StatusName != "Created")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'Created' yang dapat dipublish"
            );

        var waitingPickupStatus =
            await _procurementRepository.GetStatusByNameAsync("Waiting Pickup")
            ?? throw new InvalidOperationException("Status 'Waiting Pickup' tidak ditemukan");

        procurement.StatusId = waitingPickupStatus.StatusId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task UnpublishAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        if (procurement.Status?.StatusName != "Waiting Pickup")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'Waiting Pickup' yang dapat dibatalkan publish-nya"
            );

        var createdStatus =
            await _procurementRepository.GetStatusByNameAsync("Created")
            ?? throw new InvalidOperationException("Status 'Created' tidak ditemukan");

        procurement.StatusId = createdStatus.StatusId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task PickupAsync(string procurementId, string appoUserId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        if (string.IsNullOrWhiteSpace(appoUserId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(appoUserId));

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        if (procurement.Status?.StatusName != "Waiting Pickup")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'Waiting Pickup' yang dapat di-pickup"
            );

        var inProgressStatus =
            await _procurementRepository.GetStatusByNameAsync("In Progress")
            ?? throw new InvalidOperationException("Status 'In Progress' tidak ditemukan");

        procurement.StatusId = inProgressStatus.StatusId;
        procurement.AppoUserId = appoUserId;
        procurement.PickedUpAt = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    private async Task<Procurement> GetProcurementForWorkflowAsync(string procurementId)
    {
        return await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );
    }
}
