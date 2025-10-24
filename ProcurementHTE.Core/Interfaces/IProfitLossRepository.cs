using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProfitLossRepository
    {
        // Get Data
        Task<ProfitLoss?> GetByIdAsync(string profitLossId);
        Task<ProfitLoss?> GetByWorkOrderAsync(string woId);
        Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string woId);

        // Transaction DB
        Task StoreSelectedVendorsAsync(string woId, IEnumerable<string> vendorId);
        Task StoreProfitLossAsync(ProfitLoss profitLoss);
        Task RemoveSelectedVendorsAsync(string woId);
        Task UpdateProfitLossAsync(ProfitLoss profitLoss);
    }
}
