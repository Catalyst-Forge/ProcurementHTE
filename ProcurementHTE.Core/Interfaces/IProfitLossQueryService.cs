using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IProfitLossQueryService
{
    Task<ProfitLoss?> GetByProcurementAsync(string procurementId);
    Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(string procurementId);
    Task<ProfitLossSummaryDto> GetSummaryByProcurementAsync(string procurementId);
    Task<ProfitLossEditDto> GetEditDataAsync(string profitLossId);
    Task<ProfitLoss?> GetLatestByProcurementAsync(string procurementId);
    Task<decimal> GetTotalRevenueThisMonthAsync();
}
