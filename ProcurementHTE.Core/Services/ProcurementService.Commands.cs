using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class ProcurementService
{
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
}
