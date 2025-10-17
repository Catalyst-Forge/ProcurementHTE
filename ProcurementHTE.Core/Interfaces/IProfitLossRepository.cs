using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IProfitLossRepository
    {
        // Get Data
        Task<IEnumerable<ProfitLoss>> GetAllAsync();
        Task<ProfitLoss?> GetByIdAsync(string id);
        Task<ProfitLoss?> GetByWorkOrderAsync(string woId);
        Task<IEnumerable<ProfitLoss>> GetProfitLossByDateRangeAsync(
            DateTime startDate,
            DateTime endDate
        );

        // Transaction DB
        Task<ProfitLoss> StoreProfitLossAsync(ProfitLoss pnl);
        Task UpdateProfitLossAsync(ProfitLoss pnl);
        Task DropProfitLossAsync(string id);
    }
}
