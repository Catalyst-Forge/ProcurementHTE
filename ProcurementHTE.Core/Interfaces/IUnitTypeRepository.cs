using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces;

public interface IUnitTypeRepository
{
    Task<List<UnitType>> GetAllAsync();
    Task<List<UnitType>> GetActiveAsync();
    Task<UnitType?> GetByIdAsync(string unitTypeId);
    Task<UnitType?> GetByCodeAsync(string code);
    Task<List<UnitType>> GetByCodesAsync(List<string> codes);
}
