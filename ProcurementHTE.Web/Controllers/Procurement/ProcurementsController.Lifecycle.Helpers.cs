using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private async Task NotifyProcurementPublishedAsync(string procurementId, Procurement? procurement)
    {
        if (procurement == null)
            return;

        var currentUser = await _userManager.GetUserAsync(User);
        var publisherName = currentUser?.FullName ?? currentUser?.UserName ?? "Operator";

        await _notificationService.NotifyProcurementPublishedAsync(
            procurementId,
            procurement.ProcNum ?? "-",
            currentUser?.Id ?? "",
            publisherName,
            HttpContext.RequestAborted
        );
    }
}
