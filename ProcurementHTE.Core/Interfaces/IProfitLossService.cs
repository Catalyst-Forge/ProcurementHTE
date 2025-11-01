using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProfitLossService
    {
        // Get Data
        Task<ProfitLoss?> GetByWorkOrderAsync(string woId);
        Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string woId);
        Task<ProfitLossSummaryDto> GetSummaryByWorkOrderAsync(string woId);
        Task<ProfitLossEditDto> GetEditDataAsync(string profitLossId);
        Task<ProfitLoss?> GetLatestByWorkOrderAsync(string workOrderId);
        Task<decimal> GetTotalRevenueThisMonthAsync();

        // Transaction DB
        Task<ProfitLoss> SaveInputAndCalculateAsync(ProfitLossInputDto dto);
        Task<ProfitLoss> EditProfitLossAsync(ProfitLossUpdateDto dto);
    }
}
