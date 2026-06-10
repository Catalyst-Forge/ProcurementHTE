using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class ProfitLossService
    {
        public async Task<bool> DeleteByProcurementAsync(
            string procurementId,
            string deletedByUserId
        )
        {
            if (string.IsNullOrWhiteSpace(procurementId))
                return false;

            var pnl = await _pnlRepository.GetByProcurementAsync(procurementId);
            if (pnl == null)
                return false;

            await _pnlRepository.DeleteAsync(pnl.ProfitLossId, deletedByUserId);
            return true;
        }

        public Task<ProfitLoss?> GetByProcurementAsync(string procurementId)
        {
            return _pnlRepository.GetByProcurementAsync(procurementId);
        }

        public Task<List<ProfitLossSelectedVendor>> GetSelectedVendorsAsync(
            string procurementId
        )
        {
            return _pnlRepository.GetSelectedVendorsAsync(procurementId);
        }

        public async Task<decimal> GetTotalRevenueThisMonthAsync()
        {
            return await _pnlRepository.GetTotalRevenueThisMonthAsync();
        }

        public async Task<ProfitLoss?> GetLatestByProcurementAsync(string procurementId)
        {
            if (string.IsNullOrWhiteSpace(procurementId))
                throw new ArgumentException(
                    "ProcurementId tidak boleh kosong",
                    nameof(procurementId)
                );

            return await _pnlRepository.GetLatestByProcurementIdAsync(procurementId);
        }
    }
}
