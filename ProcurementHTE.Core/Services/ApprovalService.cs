using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
    public class ApprovalService : IApprovalService {
        private readonly IApprovalRepository _approvalRepository;
        private readonly IWorkOrderService _woService;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(IApprovalRepository approvalRepository, IWorkOrderService woService, ILogger<ApprovalService> logger) {
            _approvalRepository = approvalRepository;
            _woService = woService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<WoDocumentApprovals>> GetPendingApprovalsForUserAsync(User user) {
            return await _approvalRepository.GetPendingApprovalsForUserAsync(user);
        }

        public async Task ApproveAsync(string approvalId, string approverUserId) {
            var result = await _approvalRepository.ApproveAsync(approvalId, approverUserId);

            if (result.AllDocsApproved) {
                await _woService.MarkAsCompletedAsync(result.WorkOrderId);
                _logger.LogInformation("WO {WorkOrderId} selesai seluruh approval, status Completed", result.WorkOrderId);
            }
        }

        public async Task RejectAsync(string approvalId, string approverUserId, string? note) {
            await _approvalRepository.RejectAsync(approvalId, approverUserId, note);
        }
    }
}
