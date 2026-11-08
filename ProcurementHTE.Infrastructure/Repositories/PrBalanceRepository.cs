using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Infrastructure.Repositories;

public sealed class PrBalanceRepository(AppDbContext db) : IPrBalanceRepository
{
    public async Task<PrBalance> GetAsync(string prNumber, CancellationToken ct = default)
    {
        var value = await db.PurchaseRequisitions
            .Where(pr => pr.PrNumber == prNumber)
            .MaxAsync(pr => pr.PrValue, ct);

        // A cancelled job no longer spends anything, the same rule the dashboards follow.
        var realizations = await db.Procurements
            .Where(p =>
                p.PurchaseRequisition != null
                && p.PurchaseRequisition.PrNumber == prNumber
                && p.ProcurementStatus != ProcurementStatus.Cancelled
            )
            .Select(p => p.ProfitLosses
                .OrderByDescending(pnl => pnl.CreatedAt)
                .Select(pnl => pnl.RealizationAmount)
                .FirstOrDefault())
            .ToListAsync(ct);

        return new PrBalance(prNumber, value, realizations.Sum(amount => amount ?? 0m), realizations.Count);
    }

    public async Task<PrBalance?> GetForProcurementAsync(string procurementId, CancellationToken ct = default)
    {
        var prNumber = await db.Procurements
            .Where(p => p.ProcurementId == procurementId && p.PurchaseRequisition != null)
            .Select(p => p.PurchaseRequisition!.PrNumber)
            .FirstOrDefaultAsync(ct);

        return prNumber is null ? null : await GetAsync(prNumber, ct);
    }

    public async Task SetValueAsync(string prNumber, decimal value, CancellationToken ct = default)
    {
        var records = await db.PurchaseRequisitions.Where(pr => pr.PrNumber == prNumber).ToListAsync(ct);
        foreach (var record in records)
            record.PrValue = value;

        await db.SaveChangesAsync(ct);
    }
}
