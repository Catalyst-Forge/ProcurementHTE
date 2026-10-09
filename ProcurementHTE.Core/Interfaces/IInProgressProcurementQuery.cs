using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

public interface IInProgressProcurementQuery
{
    Task<InProgressProcurementUsage> GetUsageAsync(CancellationToken ct = default);
}
