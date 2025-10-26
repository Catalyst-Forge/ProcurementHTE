using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
    public class WoDocApprovalFlowService : IWoDocApprovalFlowService {
        private readonly IWoDocApprovalFlowRepository _flowRepository;
        private readonly ILogger<WoDocApprovalFlowService> _logger;

        public WoDocApprovalFlowService(IWoDocApprovalFlowRepository flowRepository, ILogger<WoDocApprovalFlowService> logger) {
            _flowRepository = flowRepository;
            _logger = logger;
        }

        public async Task GenerateFlowAsync(string woId, string woDocumentId) {
            var doc = await _flowRepository.GetDocumentWithWorkOrderAsync(woDocumentId);
            if (doc == null || doc.WorkOrder == null)
                throw new InvalidOperationException("Document atau Work Order tidak ditemukan");

            var woTypeDoc = await _flowRepository.GetWoTypeDocumentWithApprovalsAsync(doc.WorkOrder.WoTypeId!, doc.DocumentTypeId);

            if (woTypeDoc == null || !woTypeDoc.RequiresApproval) {
                _logger.LogInformation("Tidak ada approval yang dibutuhkan untuk doc {DocId}", woDocumentId);
                return;
            }

            var approvalsMaster = woTypeDoc.DocumentApprovals
            .OrderBy(approval => approval.Level)
            .ThenBy(approval => approval.SequenceOrder)
            .ToList();

            if (approvalsMaster.Count == 0) {
                _logger.LogWarning("Master approval kosong untuk WoTypeDoc {WoTypeDocId}", woTypeDoc.WoTypeDocumentId);
                return;
            }

            var flows = new List<WoDocumentApprovals>(approvalsMaster.Count);
            foreach (var approval in approvalsMaster) {
                flows.Add(new WoDocumentApprovals {
                    WoDocumentId = woDocumentId,
                    WorkOrderId = woId,
                    RoleId = approval.RoleId,
                    Level = approval.Level,
                    SequenceOrder = approval.SequenceOrder,
                    Status = "Pending"
                });
            }

            await _flowRepository.AddApprovalsAsync(flows);
            await _flowRepository.UpdateWoDocumentStatusAsync(woDocumentId, "Pending Approval");
            await _flowRepository.SaveChangesAsync();

            _logger.LogInformation("Generate approval flow berhasil untuk WoDocumentId={WoDocumentId}", woDocumentId);
        }
    }
}
