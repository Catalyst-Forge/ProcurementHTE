using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class NotificationService
    {
        public async Task NotifyProcurementPublishedAsync(
            string procurementId,
            string procNum,
            string publishedByUserId,
            string publishedByUserName,
            CancellationToken ct = default
        )
        {
            var title = "Procurement Baru Dipublish";
            var message =
                $"Procurement {procNum} telah dipublish oleh {publishedByUserName}. Silakan lengkapi di tabel AP-PO.";
            var actionUrl = "/ApPoPickup";

            await SendNotificationToRoleAsync(
                "AP-PO",
                title,
                message,
                NotificationTypes.ProcurementPublished,
                actionUrl,
                procurementId,
                publishedByUserId,
                ct
            );
        }
    }
}
