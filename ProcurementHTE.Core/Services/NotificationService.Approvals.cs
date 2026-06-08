using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class NotificationService
    {
        public async Task NotifyDocumentApprovedAsync(
            string prId,
            string prNumber,
            string approverRole,
            string approverUserId,
            string approverUserName,
            string nextApproverRole,
            CancellationToken ct = default
        )
        {
            try
            {
                var prInfo = await _repository.GetPrForNotificationAsync(prId, ct);
                if (prInfo == null)
                    return;

                string notificationType;
                string targetUserId;
                string title;
                string message;

                if (approverRole.Contains("Analyst", StringComparison.OrdinalIgnoreCase))
                {
                    notificationType = NotificationTypes.ApprovedByAnalyst;
                    targetUserId = prInfo.AssistantManagerUserId ?? "";
                    title = "Menunggu Approval Anda";
                    message =
                        $"PR {prNumber} telah diapprove oleh {approverUserName} (Analyst HTE & LTS). Silakan review dan approve.";
                }
                else if (approverRole.Contains("Assistant", StringComparison.OrdinalIgnoreCase))
                {
                    notificationType = NotificationTypes.ApprovedByAssistantManager;
                    targetUserId = prInfo.ManagerUserId ?? "";
                    title = "Menunggu Approval Anda";
                    message =
                        $"PR {prNumber} telah diapprove oleh {approverUserName} (Asst. Manager). Silakan review dan approve.";
                }
                else if (approverRole.Contains("Manager", StringComparison.OrdinalIgnoreCase))
                {
                    notificationType = NotificationTypes.ApprovedByManager;
                    targetUserId = prInfo.AppoUserId ?? "";
                    title = "Approval Selesai";
                    message =
                        $"PR {prNumber} telah diapprove oleh {approverUserName} (Manager). Semua approval selesai, silakan lanjutkan ke ISPA.";
                }
                else
                {
                    return;
                }

                if (string.IsNullOrEmpty(targetUserId))
                    return;

                var actionUrl = $"/PurchaseRequisitions/Details/{prId}";
                await SendNotificationAsync(
                    targetUserId,
                    title,
                    message,
                    notificationType,
                    actionUrl,
                    prId,
                    approverUserId,
                    ct
                );

                if (!string.IsNullOrEmpty(prInfo.AppoUserId) && prInfo.AppoUserId != targetUserId)
                {
                    var appoTitle = "Update Status Approval";
                    var appoMessage =
                        $"PR {prNumber} telah diapprove oleh {approverUserName} ({approverRole}).";

                    await SendNotificationAsync(
                        prInfo.AppoUserId,
                        appoTitle,
                        appoMessage,
                        notificationType,
                        actionUrl,
                        prId,
                        approverUserId,
                        ct
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send document approved notification for PR {PrId}",
                    prId
                );
            }
        }

        public async Task NotifyPrCompletedAsync(
            string prId,
            string prNumber,
            string appoUserId,
            CancellationToken ct = default
        )
        {
            var title = "PR Selesai";
            var message = $"PR {prNumber} telah selesai dengan status Done PO.";
            var actionUrl = $"/PurchaseRequisitions/Details/{prId}";

            await SendNotificationAsync(
                appoUserId,
                title,
                message,
                NotificationTypes.PrCompleted,
                actionUrl,
                prId,
                null,
                ct
            );
        }

        public async Task NotifyPrRejectedAsync(
            string prId,
            string prNumber,
            string rejectedByUserId,
            string rejectedByUserName,
            string rejectionNote,
            string appoUserId,
            CancellationToken ct = default
        )
        {
            var title = "PR Ditolak";
            var message =
                $"PR {prNumber} ditolak oleh {rejectedByUserName}. Alasan: {rejectionNote}";
            var actionUrl = $"/PurchaseRequisitions/Details/{prId}";

            await SendNotificationAsync(
                appoUserId,
                title,
                message,
                NotificationTypes.PrRejected,
                actionUrl,
                prId,
                rejectedByUserId,
                ct
            );
        }
    }
}
