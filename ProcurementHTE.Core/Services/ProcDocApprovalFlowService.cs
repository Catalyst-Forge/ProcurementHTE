using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class ProcDocApprovalFlowService : IProcDocApprovalFlowService
    {
        private readonly IProcDocApprovalFlowRepository _flowRepository;
        private readonly IProfitLossService _pnlService;
        private static readonly IReadOnlyDictionary<
            string,
            Func<Procurement, string?>
        > RoleApproverSelectors = new Dictionary<string, Func<Procurement, string?>>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            ["Manager Transport & Logistic"] = proc => proc.ManagerUserId,
            ["Assistant Manager HTE"] = proc => proc.AssistantManagerUserId,
            ["Analyst HTE & LTS"] = proc => proc.AnalystHteUserId,
            ["PIC Operations"] = proc => proc.PicOpsUserId,
            ["PIC Operation"] = proc => proc.PicOpsUserId,
            ["Operation"] = proc => proc.PicOpsUserId,
        };

        public ProcDocApprovalFlowService(
            IProcDocApprovalFlowRepository flowRepository,
            IProfitLossService pnlService,
            Microsoft.AspNetCore.Identity.RoleManager<Role> roleManager
        )
        {
            _flowRepository = flowRepository;
            _pnlService = pnlService;
        }

        public async Task GenerateFlowAsync(
            string woId,
            string procDocumentId,
            IEnumerable<string>? extraRoleNames = null
        )
        {
            var doc = await _flowRepository.GetDocumentWithProcurementAsync(procDocumentId);
            if (doc == null || doc.Procurement == null)
                throw new InvalidOperationException("Document atau Work Order tidak ditemukan");

            var docTypeName = doc.DocumentType?.Name ?? string.Empty;
            var isPnl = IsDoc(docTypeName, "Profit & Loss");

            var jobTypeDoc = await _flowRepository.GetJobTypeDocumentWithApprovalsAsync(
                doc.Procurement.JobTypeId!,
                doc.DocumentTypeId
            );

            if (jobTypeDoc == null || !jobTypeDoc.RequiresApproval)
            {
                return;
            }

            var approvalsMaster = jobTypeDoc
                .DocumentApprovals.OrderBy(approval => approval.Level)
                .ToList();

            if (approvalsMaster.Count == 0)
            {
                return;
            }

            var ct = await GetGrandTotalAsync(doc.ProcurementId);
            var conditionalRules = await _flowRepository.GetConditionalRulesAsync(
                doc.DocumentTypeId,
                doc.Procurement.JobTypeId,
                doc.Procurement.ProcurementCategory
            );
            var matchedRule = GetMatchedRule(ct, conditionalRules);

            var flows = new List<ProcDocumentApprovals>();
            var firstLevel = approvalsMaster.Min(a => a.Level);

            var isConditionalTarget =
                IsDoc(docTypeName, "Owner Estimate")
                || IsDoc(docTypeName, "RKS")
                || IsDoc(docTypeName, "Justifikasi")
                || isPnl;

            // 1) Conditional override for OE / RKS / Justifikasi
            if (matchedRule != null && !isPnl && isConditionalTarget)
            {
                var level = 1;
                if (!string.IsNullOrWhiteSpace(matchedRule.SubmitterRoleId))
                {
                    flows.Add(
                        new ProcDocumentApprovals
                        {
                            ProcDocumentId = procDocumentId,
                            ProcurementId = woId,
                            RoleId = matchedRule.SubmitterRoleId,
                            Level = level,
                            Status = "Pending",
                        }
                    );
                    level++;
                }

                if (!string.IsNullOrWhiteSpace(matchedRule.ApproverRoleId))
                {
                    flows.Add(
                        new ProcDocumentApprovals
                        {
                            ProcDocumentId = procDocumentId,
                            ProcurementId = woId,
                            RoleId = matchedRule.ApproverRoleId,
                            Level = level,
                            Status = level == 1 ? "Pending" : "Blocked",
                        }
                    );
                }
            }

            // 2) Base flow (statis) jika belum terisi
            if (flows.Count == 0)
            {
                foreach (var approval in approvalsMaster)
                {
                    var assignedApproverId = ResolveAssignedApproverId(approval, doc.Procurement);
                    flows.Add(
                        new ProcDocumentApprovals
                        {
                            ProcDocumentId = procDocumentId,
                            ProcurementId = woId,
                            RoleId = approval.RoleId,
                            AssignedApproverId = assignedApproverId,
                            Level = approval.Level,
                            Status = approval.Level == firstLevel ? "Pending" : "Blocked",
                        }
                    );
                }
            }

            // 3) PNL: tambah approver ekstra di atas chain statis (disetujui ++)
            if (
                isPnl
                && matchedRule != null
                && !string.IsNullOrWhiteSpace(matchedRule.ApproverRoleId)
            )
            {
                var existingRoleIds = flows.Select(f => f.RoleId).ToHashSet(StringComparer.Ordinal);
                if (!existingRoleIds.Contains(matchedRule.ApproverRoleId))
                {
                    var maxLevel = flows.Max(f => f.Level);
                    flows.Add(
                        new ProcDocumentApprovals
                        {
                            ProcDocumentId = procDocumentId,
                            ProcurementId = woId,
                            RoleId = matchedRule.ApproverRoleId,
                            Level = maxLevel + 1,
                            Status = "Blocked",
                        }
                    );
                }
            }

            await _flowRepository.AddApprovalsAsync(flows);
            await _flowRepository.UpdateProcDocumentStatusAsync(procDocumentId, "Pending Approval");
            await _flowRepository.SaveChangesAsync();
        }

        private async Task<decimal> GetGrandTotalAsync(string procurementId)
        {
            var pnl = await _pnlService.GetLatestByProcurementAsync(procurementId);
            return pnl?.SelectedVendorFinalOffer ?? 0m;
        }

        private static bool IsDoc(string docTypeName, string keyword) =>
            docTypeName?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

        private static DocumentApprovalRule? GetMatchedRule(
            decimal ct,
            IReadOnlyList<DocumentApprovalRule> rules
        ) =>
            rules.FirstOrDefault(r => ct >= r.MinAmount && ct <= r.MaxAmount && r.IsActive);

        private string? ResolveAssignedApproverId(
            DocumentApprovals approval,
            Procurement procurement
        )
        {
            var roleName = approval.Role?.Name;
            if (string.IsNullOrWhiteSpace(roleName) || procurement == null)
            {
                return null;
            }

            if (RoleApproverSelectors.TryGetValue(roleName, out var selector))
            {
                var userId = selector(procurement);
                return string.IsNullOrWhiteSpace(userId) ? null : userId;
            }

            return null;
        }
    }
}
