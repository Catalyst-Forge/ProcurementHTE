using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public class ProcurementService : IProcurementService
{
    private readonly IProcurementRepository _procurementRepository;
    private const string STATUS_COMPLETED = "Completed";

    public ProcurementService(IProcurementRepository procurementRepository)
    {
        _procurementRepository = procurementRepository
            ?? throw new ArgumentNullException(nameof(procurementRepository));
    }

    #region Query Methods

    public Task<PagedResult<Procurement>> GetAllProcurementWithDetailsAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct
    )
    {
        return _procurementRepository.GetAllAsync(page, pageSize, search, fields, ct);
    }

    public async Task<Procurement?> GetProcurementByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID tidak boleh kosong", nameof(id));

        return await _procurementRepository.GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<Procurement>> GetMyRecentProcurementAsync(
        string userId,
        int limit = 10,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(userId));

        if (limit <= 0)
            throw new ArgumentException("Limit harus lebih dari 0", nameof(limit));

        return await _procurementRepository.GetRecentByUserAsync(userId, limit, ct);
    }

    public Task<int> CountAllProcurementsAsync(CancellationToken ct)
    {
        return _procurementRepository.CountAsync(ct);
    }

    #endregion

    #region Lookup Methods

    public Task<JobTypes?> GetJobTypeByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID tipe pekerjaan tidak boleh kosong", nameof(id));

        return _procurementRepository.GetJobTypeByIdAsync(id);
    }

    public Task<Status?> GetStatusByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nama status tidak boleh kosong", nameof(name));

        return _procurementRepository.GetStatusByNameAsync(name);
    }

    public async Task<(List<JobTypes> JobTypes, List<Status> Statuses)>
        GetRelatedEntitiesForProcurementAsync()
    {
        var jobTypes = await _procurementRepository.GetJobTypesAsync();
        var statuses = await _procurementRepository.GetStatusesAsync();
        return (jobTypes, statuses);
    }

    #endregion

    #region Command Methods

    public async Task AddProcurementWithDetailsAsync(
        Procurement procurement,
        List<ProcDetail> details,
        List<ProcOffer> offers
    )
    {
        ValidateProcurement(procurement);

        if (!string.IsNullOrWhiteSpace(procurement.JobTypeId))
            await EnsureJobTypeExistsAsync(procurement.JobTypeId);

        procurement.CreatedAt = DateTime.UtcNow;

        var validDetails = FilterValidDetails(details);
        var validOffers = FilterValidOffers(offers);

        await _procurementRepository
            .StoreProcurementWithDetailsAsync(procurement, validDetails, validOffers);
    }

    public async Task EditProcurementAsync(
        Procurement procurement,
        string id,
        List<ProcDetail> details,
        List<ProcOffer> offers
    )
    {
        ArgumentNullException.ThrowIfNull(procurement);

        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID tidak boleh kosong", nameof(id));

        var existing = await _procurementRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Procurement dengan ID {id} tidak ditemukan");

        UpdateProcurementProperties(existing, procurement);

        if (!string.IsNullOrWhiteSpace(procurement.JobTypeId))
            await EnsureJobTypeExistsAsync(procurement.JobTypeId);

        var validDetails = FilterValidDetails(details);
        var validOffers = FilterValidOffers(offers);

        await _procurementRepository.UpdateProcurementWithDetailsAsync(existing, validDetails, validOffers);
    }

    public async Task DeleteProcurementAsync(Procurement procurement)
    {
        ArgumentNullException.ThrowIfNull(procurement);
        await _procurementRepository.DropProcurementAsync(procurement);
    }

    public async Task MarkAsCompletedAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement = await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException($"Procurement dengan ID {procurementId} tidak ditemukan");

        var completedStatus = await GetCompletedStatusAsync();
        procurement.StatusId = completedStatus.StatusId;
        procurement.CompletedAt = DateTime.UtcNow;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    #endregion

    #region Private Helper Methods

    private static void ValidateProcurement(Procurement procurement)
    {
        ArgumentNullException.ThrowIfNull(procurement);

        if (procurement.StatusId <= 0)
            throw new ArgumentException("Status harus dipilih", nameof(procurement.StatusId));
    }

    private async Task EnsureJobTypeExistsAsync(string jobTypeId)
    {
        _ = await _procurementRepository.GetJobTypeByIdAsync(jobTypeId)
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
        return (offers ?? [])
            .Where(o => !string.IsNullOrWhiteSpace(o.ItemPenawaran))
            .ToList();
    }

    private static void UpdateProcurementProperties(Procurement existing, Procurement updated)
    {
        existing.JobTypeOther = updated.JobTypeOther;
        existing.ContractType = updated.ContractType;
        existing.JobName = updated.JobName;
        existing.StartDate = updated.StartDate;
        existing.EndDate = updated.EndDate;
        existing.ProjectRegion = updated.ProjectRegion;
        existing.AccrualAmount = updated.AccrualAmount;
        existing.RealizationAmount = updated.RealizationAmount;
        existing.PotentialAccrualDate = updated.PotentialAccrualDate;
        existing.SpkNumber = updated.SpkNumber;
        existing.SpmpNumber = updated.SpmpNumber;
        existing.MemoNumber = updated.MemoNumber;
        existing.OeNumber = updated.OeNumber;
        existing.RaNumber = updated.RaNumber;
        existing.ProjectCode = updated.ProjectCode;
        existing.LtcName = updated.LtcName;
        existing.Note = updated.Note;
        existing.JobType = updated.JobType;
        existing.JobTypeId = updated.JobTypeId;
        existing.PicOpsUserId = updated.PicOpsUserId;
        existing.AnalystHteUserId = updated.AnalystHteUserId;
        existing.AssistantManagerUserId = updated.AssistantManagerUserId;
        existing.ManagerUserId = updated.ManagerUserId;
        existing.UpdatedAt = DateTime.UtcNow;

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

    #endregion
}
