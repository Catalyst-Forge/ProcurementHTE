namespace ProcurementHTE.Web.Controllers.PR;

public partial class PurchaseRequisitionsController
{
    // Empty keeps the value the number already has, so a later PR for it need not repeat it.
    private async Task ApplyPrValueAsync(string prNumber, decimal? value)
    {
        if (value is { } prValue)
            await _prBalance.SetValueAsync(prNumber, prValue, HttpContext.RequestAborted);
    }

    private async Task WarnIfPrOverdrawnAsync(string prNumber)
    {
        var warning = (await _prBalance.GetAsync(prNumber, HttpContext.RequestAborted)).OverdrawnWarning;
        if (warning is not null)
            TempData["WarningMessage"] = warning;
    }
}
