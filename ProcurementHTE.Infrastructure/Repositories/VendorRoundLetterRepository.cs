using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public class VendorRoundLetterRepository : IVendorRoundLetterRepository
{
    private readonly AppDbContext _context;

    public VendorRoundLetterRepository(AppDbContext context) => _context = context;

    public async Task<VendorRoundLetter?> GetAsync(string procurementId, string vendorId, int round)
    {
        return await _context
            .VendorRoundLetters.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ProcurementId == procurementId && x.VendorId == vendorId && x.Round == round
            );
    }

    public async Task<IReadOnlyList<VendorRoundLetter>> ListByProcurementAsync(string procurementId)
    {
        return await _context
            .VendorRoundLetters.AsNoTracking()
            .Where(x => x.ProcurementId == procurementId)
            .Include(x => x.ProcDocument)
            .Include(x => x.Vendor)
            .OrderBy(x => x.Vendor.VendorName)
            .ThenBy(x => x.Round)
            .ToListAsync();
    }

    public async Task AddOrUpdateAsync(VendorRoundLetter entity)
    {
        var existing = await _context
            .VendorRoundLetters
            .FirstOrDefaultAsync(x =>
                x.ProcurementId == entity.ProcurementId
                && x.VendorId == entity.VendorId
                && x.Round == entity.Round
            );

        if (existing == null)
        {
            await _context.VendorRoundLetters.AddAsync(entity);
        }
        else
        {
            existing.ProcDocumentId = entity.ProcDocumentId;
            existing.LetterNumber = entity.LetterNumber;
            existing.ProfitLossId = entity.ProfitLossId;
            existing.CreatedAt = entity.CreatedAt;
            existing.CreatedByUserId = entity.CreatedByUserId;
            _context.VendorRoundLetters.Update(existing);
        }
    }

    public async Task UpdateProfitLossLinkAsync(
        string procurementId,
        string vendorId,
        int round,
        string? profitLossId,
        string? letterNumber
    )
    {
        var existing = await _context
            .VendorRoundLetters
            .FirstOrDefaultAsync(x =>
                x.ProcurementId == procurementId && x.VendorId == vendorId && x.Round == round
            );

        if (existing == null)
            return;

        existing.ProfitLossId = profitLossId;
        if (!string.IsNullOrWhiteSpace(letterNumber))
            existing.LetterNumber = letterNumber;

        _context.VendorRoundLetters.Update(existing);
    }

    public async Task DeleteByProcDocumentIdAsync(string procDocumentId, CancellationToken ct = default)
    {
        var entity = await _context
            .VendorRoundLetters
            .FirstOrDefaultAsync(x => x.ProcDocumentId == procDocumentId, ct);
        if (entity != null)
        {
            _context.VendorRoundLetters.Remove(entity);
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
