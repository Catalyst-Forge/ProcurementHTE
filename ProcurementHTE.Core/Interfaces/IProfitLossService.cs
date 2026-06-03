using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProfitLossService
    {
        // Get Data
        Task<ProfitLoss?> GetByProcurementAsync(string procurementId);
        Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string procurementId);
        Task<ProfitLossSummaryDto> GetSummaryByProcurementAsync(string procurementId);
        Task<ProfitLossEditDto> GetEditDataAsync(string profitLossId);
        Task<ProfitLoss?> GetLatestByProcurementAsync(string procurementId);
        Task<decimal> GetTotalRevenueThisMonthAsync();

        // Transaction DB
        Task<ProfitLoss> SaveInputAndCalculateAsync(ProfitLossInputDto dto);
        Task<ProfitLoss> EditProfitLossAsync(ProfitLossUpdateDto dto);
        
        // Delete
        Task<bool> DeleteByProcurementAsync(string procurementId, string deletedByUserId);
    }
}
