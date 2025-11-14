using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProfitLossRepository
    {
        // Get Data
        Task<ProfitLoss?> GetByIdAsync(string profitLossId);
        Task<ProfitLoss?> GetByProcurementAsync(string woId);
        Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string woId);
        Task<ProfitLoss?> GetLatestByProcurementIdAsync(string procurementId);
        Task<decimal> GetTotalRevenueThisMonthAsync();
        Task<IReadOnlyList<RevenuePerMonthDto>> GetRevenuePerMonthAsync(int year);

        // Transaction DB
        Task StoreSelectedVendorsAsync(string woId, IEnumerable<string> vendorId);
        Task StoreProfitLossAsync(ProfitLoss profitLoss);
        Task RemoveSelectedVendorsAsync(string woId);
        Task UpdateProfitLossAsync(ProfitLoss profitLoss);
    }
}
