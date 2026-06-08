using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementService
{
    public async Task PickupForApInvoiceAsync(string procurementId, string apInvoiceUserId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        if (string.IsNullOrWhiteSpace(apInvoiceUserId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(apInvoiceUserId));

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        if (string.IsNullOrEmpty(procurement.AppoUserId))
            throw new InvalidOperationException(
                "Procurement harus di-pickup oleh AP-PO terlebih dahulu"
            );

        if (!string.IsNullOrEmpty(procurement.ApInvoiceUserId))
            throw new InvalidOperationException("Procurement sudah di-pickup oleh AP-Invoice");

        procurement.ApInvoiceUserId = apInvoiceUserId;
        procurement.ApInvoicePickedUpAt = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
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

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        procurement.SANo = saNo;
        procurement.SP3No = sp3No;
        procurement.ApInvoiceUserId = filledByUserId;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _procurementRepository.UpdateProcurementAsync(procurement);
    }

    public async Task PickupForArAsync(string procurementId, string arUserId)
    {
        if (string.IsNullOrWhiteSpace(procurementId))
            throw new ArgumentException("ID procurement tidak boleh kosong", nameof(procurementId));

        if (string.IsNullOrWhiteSpace(arUserId))
            throw new ArgumentException("User ID tidak boleh kosong", nameof(arUserId));

        var procurement = await GetProcurementForWorkflowAsync(procurementId);

        if (!string.IsNullOrEmpty(procurement.ArUserId))
            throw new InvalidOperationException("Procurement sudah di-pickup oleh AR");

        procurement.ArUserId = arUserId;
        procurement.ArPickedUpAt = _timeProvider.GetUtcNow().UtcDateTime;
        procurement.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
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
}
