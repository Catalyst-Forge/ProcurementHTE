using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Utils;

namespace ProcurementHTE.Core.Services;

public class PurchaseRequisitionService : IPurchaseRequisitionQueryService, IPurchaseRequisitionCommandService
{
    private readonly IPurchaseRequisitionRepository _purchaseRequisitionRepository;
    private readonly IProcurementTrackingService _procurementTrackingService;
    private readonly TimeProvider _timeProvider;
    private const string PR_PREFIX = "PR";

    public PurchaseRequisitionService(
        IPurchaseRequisitionRepository purchaseRequisitionRepository,
        IProcurementTrackingService procurementTrackingService,
        TimeProvider timeProvider)
    {
        _purchaseRequisitionRepository =
            purchaseRequisitionRepository
            ?? throw new ArgumentNullException(nameof(purchaseRequisitionRepository));
        _procurementTrackingService =
            procurementTrackingService
            ?? throw new ArgumentNullException(nameof(procurementTrackingService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    #region Query Methods

    public Task<PagedResult<PurchaseRequisition>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct = default
    )
    {
        return _purchaseRequisitionRepository.GetAllAsync(page, pageSize, search, fields, ct);
    }

    public async Task<PurchaseRequisition?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID tidak boleh kosong", nameof(id));

        return await _purchaseRequisitionRepository.GetByIdAsync(id, ct);
    }

    public async Task<PurchaseRequisition?> GetByIdWithProcurementsAsync(
        string id,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID tidak boleh kosong", nameof(id));

        return await _purchaseRequisitionRepository.GetByIdWithProcurementsAsync(id, ct);
    }

    public Task<IReadOnlyList<PurchaseRequisition>> GetRecentAsync(
        int limit = 10,
        CancellationToken ct = default
    )
    {
        if (limit <= 0)
            throw new ArgumentException("Limit harus lebih dari 0", nameof(limit));

        return _purchaseRequisitionRepository.GetRecentAsync(limit, ct);
    }

    public Task<int> CountAsync(CancellationToken ct = default)
    {
        return _purchaseRequisitionRepository.CountAsync(ct);
    }

    public Task<bool> IsPrNumberExistsAsync(string prNumber, string? excludePrId = null, CancellationToken ct = default)
    {
        return _purchaseRequisitionRepository.IsPrNumberExistsAsync(prNumber, excludePrId, ct);
    }

    #endregion

    #region Command Methods

    public async Task<PurchaseRequisition> CreateAsync(
        PurchaseRequisition purchaseRequisition,
        IEnumerable<string> procurementIds,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(purchaseRequisition);

        // Generate PR Number if not provided
        if (string.IsNullOrWhiteSpace(purchaseRequisition.PrNumber))
        {
            var lastPrNumber = await _purchaseRequisitionRepository.GetLastPrNumberAsync(PR_PREFIX, ct);
            purchaseRequisition.PrNumber = SequenceNumberGenerator.NumId(PR_PREFIX, lastPrNumber);
        }

        purchaseRequisition.CreatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        // Set initial status
        purchaseRequisition.Status = PurchaseRequisitionStatus.OnCreateDP3;

        // Add initial status history - On Create DP3 (APPO)
        // Don't set PrId explicitly - EF will set it from the navigation property
        purchaseRequisition.StatusHistories.Add(new PurchaseRequisitionStatusHistory
        {
            Status = PurchaseRequisitionStatus.OnCreateDP3,
            ChangedAt = purchaseRequisition.CreatedAt,
            ChangedByUserId = purchaseRequisition.CreatedByUserId,
            Note = "PR created"
        });

        // Create the purchase requisition
        await _purchaseRequisitionRepository.CreateAsync(purchaseRequisition, ct);

        // Link procurements to this PR
        var procIds = procurementIds?.ToList() ?? [];
        if (procIds.Count > 0)
        {
            await _purchaseRequisitionRepository.LinkProcurementsAsync(
                purchaseRequisition.PrId,
                procIds,
                ct
            );

            // Recalculate PR status based on linked procurements
            await _procurementTrackingService.RecalculatePrStatusAsync(purchaseRequisition.PrId, ct);
        }

        return purchaseRequisition;
    }

    public async Task UpdateAsync(
        PurchaseRequisition purchaseRequisition,
        IEnumerable<string>? procurementIds = null,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(purchaseRequisition);

        var existing =
            await _purchaseRequisitionRepository.GetByIdAsync(purchaseRequisition.PrId, ct)
            ?? throw new KeyNotFoundException(
                $"Purchase Requisition dengan ID {purchaseRequisition.PrId} tidak ditemukan"
            );

        // Update properties
        existing.PrNumber = purchaseRequisition.PrNumber;
        existing.RequestDate = purchaseRequisition.RequestDate;
        existing.Description = purchaseRequisition.Description;
        existing.DocumentFileName = purchaseRequisition.DocumentFileName;
        existing.DocumentFilePath = purchaseRequisition.DocumentFilePath;
        existing.DocumentContentType = purchaseRequisition.DocumentContentType;
        existing.DocumentFileSize = purchaseRequisition.DocumentFileSize;
        existing.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await _purchaseRequisitionRepository.UpdateAsync(existing, ct);

        // Update linked procurements if provided
        if (procurementIds != null)
        {
            // Unlink all existing procurements
            await _purchaseRequisitionRepository.UnlinkAllProcurementsAsync(existing.PrId, ct);

            // Link new procurements
            var procIds = procurementIds.ToList();
            if (procIds.Count > 0)
            {
                await _purchaseRequisitionRepository.LinkProcurementsAsync(existing.PrId, procIds, ct);
            }

            // Recalculate PR status based on linked procurements
            await _procurementTrackingService.RecalculatePrStatusAsync(existing.PrId, ct);
        }
    }

    public async Task DeleteAsync(string id, string deletedByUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID tidak boleh kosong", nameof(id));

        if (string.IsNullOrWhiteSpace(deletedByUserId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(deletedByUserId));

        var existing =
            await _purchaseRequisitionRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException(
                $"Purchase Requisition dengan ID {id} tidak ditemukan"
            );

        // Unlink all procurements first
        await _purchaseRequisitionRepository.UnlinkAllProcurementsAsync(id, ct);

        // Delete the purchase requisition
        await _purchaseRequisitionRepository.DeleteAsync(existing, deletedByUserId, ct);
    }

    #endregion
}
