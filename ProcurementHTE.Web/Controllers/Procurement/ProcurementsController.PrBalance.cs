namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    // Realization draws from the job's PR. Going over its value is allowed, but said out loud.
    private async Task WarnIfPrOverdrawnAsync(string procurementId)
    {
        var balance = await _prBalance.GetForProcurementAsync(procurementId, HttpContext.RequestAborted);
        if (balance?.OverdrawnWarning is { } warning)
            TempData["WarningMessage"] = warning;
    }
}
