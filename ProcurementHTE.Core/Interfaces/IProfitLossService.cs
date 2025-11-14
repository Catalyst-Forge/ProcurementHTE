using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProfitLossService
    {
        // Get Data
        Task<ProfitLoss?> GetByProcurementAsync(string woId);
        Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string woId);
        Task<ProfitLossSummaryDto> GetSummaryByProcurementAsync(string woId);
        Task<ProfitLossEditDto> GetEditDataAsync(string profitLossId);
        Task<ProfitLoss?> GetLatestByProcurementAsync(string procurementId);
        Task<decimal> GetTotalRevenueThisMonthAsync();

        // Transaction DB
        Task<ProfitLoss> SaveInputAndCalculateAsync(ProfitLossInputDto dto);
        Task<ProfitLoss> EditProfitLossAsync(ProfitLossUpdateDto dto);
    }
}
