using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public class PurchaseRequisitionRepository : IPurchaseRequisitionRepository
{
    private readonly AppDbContext _context;

    public PurchaseRequisitionRepository(AppDbContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    #region Query Methods

    public Task<PagedResult<PurchaseRequisition>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        ISet<string> fields,
        CancellationToken ct = default
    )
    {
        var query = _context
            .PurchaseRequisitions.Include(pr => pr.CreatedByUser)
            .Include(pr => pr.Procurements)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search) && fields.Count > 0)
        {
            var s = search.Trim();
            bool byPrNumber = fields.Contains("PrNumber", StringComparer.OrdinalIgnoreCase);
            bool byDescription = fields.Contains("Description", StringComparer.OrdinalIgnoreCase);

            query = query.Where(pr =>
                (byPrNumber && pr.PrNumber != null && EF.Functions.Like(pr.PrNumber, $"%{s}%"))
                || (
                    byDescription
                    && pr.Description != null
                    && EF.Functions.Like(pr.Description, $"%{s}%")
                )
            );
        }

        return query.ToPagedResultAsync(
            page,
            pageSize,
            orderBy: q => q.OrderByDescending(pr => pr.CreatedAt),
            ct: ct
        );
    }

    public async Task<PurchaseRequisition?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _context
            .PurchaseRequisitions.Include(pr => pr.CreatedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(pr => pr.PrId == id, ct);
    }

    public async Task<PurchaseRequisition?> GetByIdWithProcurementsAsync(
        string id,
        CancellationToken ct = default
    )
    {
        return await _context
            .PurchaseRequisitions.Include(pr => pr.CreatedByUser)
            .Include(pr => pr.Procurements)
            .ThenInclude(p => p.JobType)
            .Include(pr => pr.Procurements)
            .ThenInclude(p => p.Status)
            .Include(pr => pr.Procurements)
            .ThenInclude(p => p.ProfitLosses)
            .ThenInclude(pl => pl.SelectedVendor)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(pr => pr.PrId == id, ct);
    }

    public async Task<IReadOnlyList<PurchaseRequisition>> GetRecentAsync(
        int limit,
        CancellationToken ct = default
    )
    {
        return await _context
            .PurchaseRequisitions.Include(pr => pr.CreatedByUser)
            .Include(pr => pr.Procurements)
            .OrderByDescending(pr => pr.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct = default)
    {
        return _context.PurchaseRequisitions.CountAsync(ct);
    }

    public async Task<string?> GetLastPrNumberAsync(string prefix, CancellationToken ct = default)
    {
        return await _context
            .PurchaseRequisitions.Where(pr => pr.PrNumber.StartsWith(prefix))
            .OrderByDescending(pr => pr.PrNumber)
            .Select(pr => pr.PrNumber)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    #endregion

    #region Command Methods

    public async Task CreateAsync(
        PurchaseRequisition purchaseRequisition,
        CancellationToken ct = default
    )
    {
        await _context.PurchaseRequisitions.AddAsync(purchaseRequisition, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(
        PurchaseRequisition purchaseRequisition,
        CancellationToken ct = default
    )
    {
        _context.Entry(purchaseRequisition).State = EntityState.Modified;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(
        PurchaseRequisition purchaseRequisition,
        CancellationToken ct = default
    )
    {
        // Soft delete: mark as deleted instead of removing
        var entityToDelete = await _context.PurchaseRequisitions.FirstOrDefaultAsync(
            pr => pr.PrId == purchaseRequisition.PrId,
            ct
        );

        if (entityToDelete != null)
        {
            entityToDelete.IsDeleted = true;
            entityToDelete.DeletedAt = DateTime.UtcNow;
            // Note: DeletedBy should be set by the service layer with current user ID
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task LinkProcurementsAsync(
        string prId,
        IEnumerable<string> procurementIds,
        CancellationToken ct = default
    )
    {
        var procurementId = procurementIds.ToList();
        if (procurementId.Count == 0)
            return;

        var procurements = await _context
            .Procurements.Where(p => procurementId.Contains(p.ProcurementId))
            .ToListAsync(ct);

        foreach (var procurement in procurements)
        {
            procurement.PrId = prId;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task UnlinkAllProcurementsAsync(string prId, CancellationToken ct = default)
    {
        var procurements = await _context.Procurements.Where(p => p.PrId == prId).ToListAsync(ct);

        foreach (var procurement in procurements)
        {
            procurement.PrId = null;
        }

        await _context.SaveChangesAsync(ct);
    }

    #endregion
}
