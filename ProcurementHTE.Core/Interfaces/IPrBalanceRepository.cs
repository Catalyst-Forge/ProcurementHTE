using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IPrBalanceRepository
{
    Task<PrBalance> GetAsync(string prNumber, CancellationToken ct = default);

    /// <summary>The balance of the PR a job draws from, or null when the job has no PR.</summary>
    Task<PrBalance?> GetForProcurementAsync(string procurementId, CancellationToken ct = default);

    /// <summary>Sets the value on every PR record with this number.</summary>
    Task SetValueAsync(string prNumber, decimal value, CancellationToken ct = default);
}
