using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementService
{
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

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        procurement.NoAccrual = noAccrual;
        procurement.PotensiAccrual = potensiAccrual;
        procurement.StatusAccrual = statusAccrual;
        procurement.AccrualFilledByUserId = filledByUserId;
        procurement.AccrualFilledAt = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

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
        return _procurementRepository.GetProcurementsForAccrualAsync(
            page,
            pageSize,
            search,
            filter,
            ct
        );
    }
}
