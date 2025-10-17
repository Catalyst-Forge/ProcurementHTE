using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProfitLossService
    {
        // Get Data
        Task<IEnumerable<ProfitLoss>> GetAllProfitLossAsync();
        Task<ProfitLoss?> GetProfitLossByIdAsync(string id);
        Task<ProfitLoss?> GetProfitLossWithWorkOrderAsync(string woId);

        // Transaction DB
        Task<ProfitLoss> CreateProfitLossWithOffersAsync(IEnumerable<VendorOfferInputDto> vendorOffers, CreateProfitLossInputDto pnlInput);
        Task<ProfitLoss> UpdateProfitLossAsync(string id, UpdateProfitLossDto dto);
        Task<ProfitLoss> CalculateProfitLossAsync(string id, string selectedVendorOfferId);
        Task DeleteProfitLossAsync(string id);
    }
}
