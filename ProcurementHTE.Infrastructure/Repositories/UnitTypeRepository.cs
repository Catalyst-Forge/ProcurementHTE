using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public class UnitTypeRepository : IUnitTypeRepository
{
    private readonly AppDbContext _context;

    public UnitTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UnitType>> GetAllAsync()
    {
        return await _context.UnitTypes
            .OrderBy(u => u.SortOrder)
            .ToListAsync();
    }

    public async Task<List<UnitType>> GetActiveAsync()
    {
        return await _context.UnitTypes
            .Where(u => u.IsActive)
            .OrderBy(u => u.SortOrder)
            .ToListAsync();
    }

    public async Task<UnitType?> GetByIdAsync(string unitTypeId)
    {
        return await _context.UnitTypes
            .FirstOrDefaultAsync(u => u.UnitTypeId == unitTypeId);
    }

    public async Task<UnitType?> GetByCodeAsync(string code)
    {
        return await _context.UnitTypes
            .FirstOrDefaultAsync(u => u.Code == code);
    }

    public async Task<List<UnitType>> GetByCodesAsync(List<string> codes)
    {
        return await _context.UnitTypes
            .Where(u => codes.Contains(u.Code))
            .OrderBy(u => u.SortOrder)
            .ToListAsync();
    }
}
