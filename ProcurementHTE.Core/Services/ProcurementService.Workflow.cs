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

        // Nobody picks a published job up any more: it goes straight to the finance tables.
        var inProgressStatus =
            await _procurementRepository.GetStatusByNameAsync("In Progress")
            ?? throw new InvalidOperationException("Status 'In Progress' tidak ditemukan");

        procurement.StatusId = inProgressStatus.StatusId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task UnpublishAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        if (!CanUnpublish(procurement))
            throw new InvalidOperationException(
                "Publish hanya bisa dibatalkan sebelum procurement dikirim untuk approval"
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

    public async Task UpdateRaNumberAsync(string procurementId, string raNumber)
    {
        if (string.IsNullOrWhiteSpace(raNumber))
            throw new ArgumentException("Nomor RA tidak boleh kosong", nameof(raNumber));

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        if (!procurement.CanChangeRaNumber)
            throw new InvalidOperationException("Nomor RA pekerjaan ini tidak bisa diubah");

        procurement.RaNumber = raNumber.Trim();
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

    /// <summary>
    /// Only Admin changes a job that is already published (the client's rule for running jobs);
    /// everyone else edits it while it is Draft or Created. A cancelled job is not edited.
    /// </summary>
    public static bool CanEdit(Procurement procurement, bool isAdmin)
    {
        var status = procurement.Status?.StatusName;
        if (procurement.ProcurementStatus == Enums.ProcurementStatus.Cancelled
            || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return false;

        return isAdmin
            || string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Created", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A published job can go back to Created until AP-PO sends it for approval.</summary>
    public static bool CanUnpublish(Procurement procurement) =>
        procurement.ProcurementStatus == Enums.ProcurementStatus.OnCreateDP3
        && procurement.Status?.StatusName is "In Progress" or "Waiting Pickup";
}
