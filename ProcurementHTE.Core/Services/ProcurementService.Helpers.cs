using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementService
{
    private static void ValidateProcurement(Procurement procurement)
    {
        ArgumentNullException.ThrowIfNull(procurement);

        if (procurement.StatusId <= 0)
            throw new ArgumentException("Status harus dipilih", nameof(procurement.StatusId));
    }

    private async Task EnsureJobTypeExistsAsync(string jobTypeId)
    {
        _ =
            await _procurementRepository.GetJobTypeByIdAsync(jobTypeId)
            ?? throw new KeyNotFoundException($"Job type dengan Id {jobTypeId} tidak ditemukan");
    }

    private static List<ProcDetail> FilterValidDetails(List<ProcDetail>? details)
    {
        return (details ?? [])
            .Where(detail =>
                !string.IsNullOrWhiteSpace(detail.ItemName)
                && detail.Quantity.HasValue
                && detail.Quantity.Value > 0
            )
            .ToList();
    }

    private static List<ProcOffer> FilterValidOffers(List<ProcOffer>? offers)
    {
        return (offers ?? []).Where(o => !string.IsNullOrWhiteSpace(o.ItemPenawaran)).ToList();
    }

    private static void UpdateProcurementProperties(Procurement existing, Procurement updated)
    {
        existing.ContractType = updated.ContractType;
        existing.ProcurementCategory = updated.ProcurementCategory;
        existing.JobName = updated.JobName;
        existing.DocumentDate = updated.DocumentDate;
        existing.StartDate = updated.StartDate;
        existing.EndDate = updated.EndDate;
        existing.ProjectRegion = updated.ProjectRegion;
        existing.PotentialAccrualDate = updated.PotentialAccrualDate;
        existing.SpkNumber = updated.SpkNumber;
        existing.Wonum = updated.Wonum;
        existing.SpmpNumber = updated.SpmpNumber;
        existing.MemoNumber = updated.MemoNumber;
        existing.OeNumber = updated.OeNumber;
        existing.RaNumber = updated.RaNumber;
        existing.ProjectCode = updated.ProjectCode;
        existing.LtcName = updated.LtcName;
        existing.Note = updated.Note;
        existing.PicOpsUserId = updated.PicOpsUserId;
        existing.AnalystHteUserId = updated.AnalystHteUserId;
        existing.AssistantManagerUserId = updated.AssistantManagerUserId;
        existing.ManagerUserId = updated.ManagerUserId;
        existing.AnalystHtePjs = updated.AnalystHtePjs;
        existing.AssistantManagerPjs = updated.AssistantManagerPjs;
        existing.ManagerPjs = updated.ManagerPjs;
        existing.VicePresidentPjs = updated.VicePresidentPjs;
        existing.OperationDirectorPjs = updated.OperationDirectorPjs;
        existing.PresidentDirectorPjs = updated.PresidentDirectorPjs;
        existing.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(updated.JobTypeId))
            existing.JobTypeId = updated.JobTypeId;

        if (updated.StatusId > 0)
            existing.StatusId = updated.StatusId;
    }

    private async Task<Status> GetCompletedStatusAsync()
    {
        var statuses = await _procurementRepository.GetStatusesAsync();
        var completedStatus = statuses
            .Where(status =>
                status.StatusName.Equals(STATUS_COMPLETED, StringComparison.OrdinalIgnoreCase)
            )
            .OrderByDescending(status => status.StatusId)
            .FirstOrDefault();

        if (completedStatus == null)
            throw new InvalidOperationException(
                $"Status '{STATUS_COMPLETED}' tidak ditemukan dalam database"
            );

        return completedStatus;
    }
}
