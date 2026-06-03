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
        _procurementRepository =
            procurementRepository ?? throw new ArgumentNullException(nameof(procurementRepository));
    }

    #region Query Methods

    public Task<PagedResult<Procurement>> GetAllProcurementWithDetailsAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct,
        string? userId
    )
    {
        return _procurementRepository.GetAllAsync(page, pageSize, search, fields, ct, userId);
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

    public Task<PagedResult<Procurement>> GetProcurementsForAppoApprovalAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct
    )
    {
        return _procurementRepository.GetProcurementsForAppoApprovalAsync(
            page,
            pageSize,
            search,
            fields,
            ct
        );
    }

    public Task<PagedResult<Procurement>> GetMyAppoPickupsAsync(
        string appoUserId,
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct
    )
    {
        return _procurementRepository.GetMyAppoPickupsAsync(
            appoUserId,
            page,
            pageSize,
            search,
            fields,
            ct
        );
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

    public async Task<(
        List<JobTypes> JobTypes,
        List<Status> Statuses
    )> GetRelatedEntitiesForProcurementAsync()
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

        await _procurementRepository.StoreProcurementWithDetailsAsync(
            procurement,
            validDetails,
            validOffers
        );
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

        var existing =
            await _procurementRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Procurement dengan ID {id} tidak ditemukan");

        UpdateProcurementProperties(existing, procurement);

        if (!string.IsNullOrWhiteSpace(procurement.JobTypeId))
            await EnsureJobTypeExistsAsync(procurement.JobTypeId);

        var validDetails = FilterValidDetails(details);
        var validOffers = FilterValidOffers(offers);

        await _procurementRepository.UpdateProcurementWithDetailsAsync(
            existing,
            validDetails,
            validOffers
        );
    }

    public async Task DeleteProcurementAsync(Procurement procurement, string deletedByUserId)
    {
        ArgumentNullException.ThrowIfNull(procurement);
        if (string.IsNullOrWhiteSpace(deletedByUserId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(deletedByUserId));
        
        await _procurementRepository.DeleteAsync(procurement, deletedByUserId);
    }

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
        procurement.CompletedAt = DateTime.UtcNow;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task ApproveByAppoAsync(string procurementId, string appoUserId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        if (string.IsNullOrWhiteSpace(appoUserId))
            throw new ArgumentException("User ID AP-PO tidak boleh kosong", nameof(appoUserId));

        var procurement =
            await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );

        if (procurement.Status?.StatusName != "In Progress")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'In Progress' yang dapat diapprove"
            );

        if (!string.IsNullOrWhiteSpace(procurement.AppoUserId))
            throw new InvalidOperationException("Procurement ini sudah di-approve oleh AP-PO");

        procurement.AppoUserId = appoUserId;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task RejectByAppoAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement =
            await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );

        if (procurement.Status?.StatusName != "In Progress")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'In Progress' yang dapat direject"
            );

        var createdStatus =
            await _procurementRepository.GetStatusByNameAsync("Created")
            ?? throw new InvalidOperationException("Status 'Created' tidak ditemukan");

        procurement.StatusId = createdStatus.StatusId;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task PublishAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement =
            await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );

        if (procurement.Status?.StatusName != "Created")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'Created' yang dapat dipublish"
            );

        var waitingPickupStatus =
            await _procurementRepository.GetStatusByNameAsync("Waiting Pickup")
            ?? throw new InvalidOperationException("Status 'Waiting Pickup' tidak ditemukan");

        procurement.StatusId = waitingPickupStatus.StatusId;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task UnpublishAsync(string procurementId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement =
            await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );

        if (procurement.Status?.StatusName != "Waiting Pickup")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'Waiting Pickup' yang dapat dibatalkan publish-nya"
            );

        var createdStatus =
            await _procurementRepository.GetStatusByNameAsync("Created")
            ?? throw new InvalidOperationException("Status 'Created' tidak ditemukan");

        procurement.StatusId = createdStatus.StatusId;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task PickupAsync(string procurementId, string appoUserId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        if (string.IsNullOrWhiteSpace(appoUserId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(appoUserId));

        var procurement =
            await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );

        if (procurement.Status?.StatusName != "Waiting Pickup")
            throw new InvalidOperationException(
                "Hanya procurement dengan status 'Waiting Pickup' yang dapat di-pickup"
            );

        var inProgressStatus =
            await _procurementRepository.GetStatusByNameAsync("In Progress")
            ?? throw new InvalidOperationException("Status 'In Progress' tidak ditemukan");

        procurement.StatusId = inProgressStatus.StatusId;
        procurement.AppoUserId = appoUserId;
        procurement.PickedUpAt = DateTime.UtcNow;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task PickupForApInvoiceAsync(string procurementId, string apInvoiceUserId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        if (string.IsNullOrWhiteSpace(apInvoiceUserId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(apInvoiceUserId));

        var procurement =
            await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );

        // Must be picked up by AP-PO first
        if (string.IsNullOrEmpty(procurement.AppoUserId))
            throw new InvalidOperationException(
                "Procurement harus di-pickup oleh AP-PO terlebih dahulu"
            );

        // Cannot be picked up twice
        if (!string.IsNullOrEmpty(procurement.ApInvoiceUserId))
            throw new InvalidOperationException("Procurement sudah di-pickup oleh AP-Invoice");

        procurement.ApInvoiceUserId = apInvoiceUserId;
        procurement.ApInvoicePickedUpAt = DateTime.UtcNow;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task UpdateInvoiceDataAsync(
        string procurementId,
        string? saNo,
        string? sp3No,
        string filledByUserId
    )
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        var procurement =
            await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );

        procurement.SANo = saNo;
        procurement.SP3No = sp3No;
        procurement.ApInvoiceUserId = filledByUserId;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task PickupForArAsync(string procurementId, string arUserId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        if (string.IsNullOrWhiteSpace(arUserId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(arUserId));

        var procurement =
            await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException(
                $"Procurement dengan ID {procurementId} tidak ditemukan"
            );

        // AR dapat pickup secara paralel dengan AP-PO setelah publish
        // Tidak perlu menunggu AP-Invoice (AP-Invoice adalah yang terakhir)

        // Cannot be picked up twice
        if (!string.IsNullOrEmpty(procurement.ArUserId))
            throw new InvalidOperationException("Procurement sudah di-pickup oleh AR");

        procurement.ArUserId = arUserId;
        procurement.ArPickedUpAt = DateTime.UtcNow;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task<PagedResult<Procurement>> GetProcurementsForApInvoiceAsync(
        int page,
        int pageSize,
        string? search,
        string? filter,
        CancellationToken ct
    )
    {
        return await _procurementRepository.GetProcurementsForApInvoiceAsync(
            page,
            pageSize,
            search,
            filter,
            ct
        );
    }

    public async Task<PagedResult<Procurement>> GetMyApInvoicePickupsAsync(
        string apInvoiceUserId,
        int page,
        int pageSize,
        string? search,
        CancellationToken ct
    )
    {
        return await _procurementRepository.GetMyApInvoicePickupsAsync(
            apInvoiceUserId,
            page,
            pageSize,
            search,
            ct
        );
    }

    public async Task<PagedResult<Procurement>> GetProcurementsForArPickupAsync(
        int page,
        int pageSize,
        string? search,
        string? filter,
        CancellationToken ct
    )
    {
        return await _procurementRepository.GetProcurementsForArPickupAsync(
            page,
            pageSize,
            search,
            filter,
            ct
        );
    }

    public async Task<PagedResult<Procurement>> GetMyArPickupsAsync(
        string arUserId,
        int page,
        int pageSize,
        string? search,
        CancellationToken ct
    )
    {
        return await _procurementRepository.GetMyArPickupsAsync(
            arUserId,
            page,
            pageSize,
            search,
            ct
        );
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
        // Pjs flags
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

    #endregion

    #region Accrual Methods

    public async Task UpdateAccrualDataAsync(
        string procurementId,
        string? noAccrual,
        decimal? potensiAccrual,
        string? statusAccrual,
        string filledByUserId
    )
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        if (string.IsNullOrWhiteSpace(filledByUserId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(filledByUserId));

        var procurement = await _procurementRepository.GetByIdAsync(procurementId)
            ?? throw new KeyNotFoundException($"Procurement dengan ID {procurementId} tidak ditemukan");

        procurement.NoAccrual = noAccrual;
        procurement.PotensiAccrual = potensiAccrual;
        procurement.StatusAccrual = statusAccrual;
        procurement.AccrualFilledByUserId = filledByUserId;
        procurement.AccrualFilledAt = DateTime.UtcNow;
        procurement.UpdatedAt = DateTime.UtcNow;

        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public Task<PagedResult<Procurement>> GetProcurementsForAccrualAsync(
        int page,
        int pageSize,
        string? search,
        string? filter,
        CancellationToken ct
    )
    {
        return _procurementRepository.GetProcurementsForAccrualAsync(page, pageSize, search, filter, ct);
    }

    #endregion
}
