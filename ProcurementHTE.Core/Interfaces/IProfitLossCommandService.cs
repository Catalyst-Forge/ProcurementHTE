using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IProfitLossCommandService
{
    Task<ProfitLoss> SaveInputAndCalculateAsync(ProfitLossInputDto dto);
    Task<ProfitLoss> EditProfitLossAsync(ProfitLossUpdateDto dto);
    Task<ProfitLoss> SaveSimpleAsync(SimpleProfitLossDto dto);
    Task<bool> DeleteByProcurementAsync(string procurementId, string deletedByUserId);
}
