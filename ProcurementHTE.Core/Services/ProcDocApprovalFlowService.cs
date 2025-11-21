using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services {
    public class ProcDocApprovalFlowService : IProcDocApprovalFlowService {
        private readonly IProcDocApprovalFlowRepository _flowRepository;
        private readonly ILogger<ProcDocApprovalFlowService> _logger;
        private readonly Microsoft.AspNetCore.Identity.RoleManager<ProcurementHTE.Core.Models.Role> _roleManager;

        public ProcDocApprovalFlowService(
            IProcDocApprovalFlowRepository flowRepository,
            ILogger<ProcDocApprovalFlowService> logger,
            Microsoft.AspNetCore.Identity.RoleManager<ProcurementHTE.Core.Models.Role> roleManager)
        {
            _flowRepository = flowRepository;
            _logger = logger;
            _roleManager = roleManager;
        }

        public async Task GenerateFlowAsync(string woId, string procDocumentId, IEnumerable<string>? extraRoleNames = null) {
            var doc = await _flowRepository.GetDocumentWithProcurementAsync(procDocumentId);
            if (doc == null || doc.Procurement == null)
                throw new InvalidOperationException("Document atau Work Order tidak ditemukan");

            var jobTypeDoc = await _flowRepository.GetJobTypeDocumentWithApprovalsAsync(doc.Procurement.JobTypeId!, doc.DocumentTypeId);

            if (jobTypeDoc == null || !jobTypeDoc.RequiresApproval) {
                _logger.LogInformation("Tidak ada approval yang dibutuhkan untuk doc {DocId}", procDocumentId);
                return;
            }

            var approvalsMaster = jobTypeDoc.DocumentApprovals
            .OrderBy(approval => approval.Level)
            .ThenBy(approval => approval.SequenceOrder)
            .ToList();

            if (approvalsMaster.Count == 0) {
                _logger.LogWarning("Master approval kosong untuk JobTypeDoc {JobTypeDocId}", jobTypeDoc.JobTypeDocumentId);
                return;
            }

            var flows = new List<ProcDocumentApprovals>(approvalsMaster.Count + (extraRoleNames?.Count() ?? 0));
            foreach (var approval in approvalsMaster) {
                flows.Add(new ProcDocumentApprovals {
                    ProcDocumentId = procDocumentId,
                    ProcurementId = woId,
                    RoleId = approval.RoleId,
                    Level = approval.Level,
                    SequenceOrder = approval.SequenceOrder,
                    Status = "Pending"
                });
            }

            // Append extra role names as a new level (maxLevel + 1)
            if (extraRoleNames != null && extraRoleNames.Any()) {
                var maxLevel = approvalsMaster.Max(a => a.Level);
                int seq = 1;
                foreach (var roleName in extraRoleNames) {
                    if (string.IsNullOrWhiteSpace(roleName)) continue;
                    var role = await _roleManager.FindByNameAsync(roleName);
                    if (role == null) {
                        _logger.LogWarning("Role '{RoleName}' not found, skipping append.", roleName);
                        continue;
                    }
                    flows.Add(new ProcDocumentApprovals {
                        ProcDocumentId = procDocumentId,
                        ProcurementId = woId,
                        RoleId = role.Id,
                        Level = maxLevel + 1,
                        SequenceOrder = seq++,
                        Status = "Pending"
                    });
                }
            }

            await _flowRepository.AddApprovalsAsync(flows);
            await _flowRepository.UpdateProcDocumentStatusAsync(procDocumentId, "Pending Approval");
            await _flowRepository.SaveChangesAsync();

            _logger.LogInformation("Generate approval flow berhasil untuk ProcDocumentId={ProcDocumentId}", procDocumentId);
        }
    }
}
