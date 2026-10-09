using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Models.DTOs;

/// <summary>
/// Counts of procurements still in progress (not Done PO, not rejected) per
/// procurement type and category. Document configuration is read live, so
/// these are the procurements a config change takes effect on immediately.
/// </summary>
public sealed class InProgressProcurementUsage(
    IReadOnlyList<(string? JobTypeId, ProcurementCategory Category, int Count)> rows
)
{
    public static readonly InProgressProcurementUsage Empty = new([]);

    /// <param name="jobTypeId">Null matches every procurement type.</param>
    /// <param name="category">Null matches every category.</param>
    public int Count(string? jobTypeId, ProcurementCategory? category) =>
        rows.Where(r => (jobTypeId == null || r.JobTypeId == jobTypeId) && (category == null || r.Category == category))
            .Sum(r => r.Count);
}
