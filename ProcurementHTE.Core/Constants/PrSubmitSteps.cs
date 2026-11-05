using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Constants;

/// <summary>
/// INPRO, hardcopy and PO are submitted once per PR, so a step opens only when
/// every live job in the PR has reached it. Cancelled jobs no longer count.
/// </summary>
public static class PrSubmitSteps
{
    public static ProcurementStatus? Current(IEnumerable<ProcurementStatus> statuses)
    {
        var live = statuses.Where(s => s != ProcurementStatus.Cancelled).Distinct().ToList();

        return live.Count == 1
            && live[0]
                is ProcurementStatus.OnSubmitISPA
                    or ProcurementStatus.OnSubmitHardcopy
                    or ProcurementStatus.OnSubmitPO
            ? live[0]
            : null;
    }
}
