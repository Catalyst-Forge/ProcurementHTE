using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Authorization.Requirements;
using ProcurementHTE.Core.Authorization.Resources;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Authorization.Handlers
{
    public sealed class CanApproveProcDocumentHandler
        : AuthorizationHandler<CanApproveProcDocumentRequirement, ApproveDocContext>
    {
        private readonly IProcDocumentRepository _docRepository;
        private readonly IProcDocumentApprovalRepository _procDocApprovalRepository;
        private readonly IJobTypeDocumentRepository _configRepository;
        private readonly RoleManager<Role> _roleManager;

        public CanApproveProcDocumentHandler(
            IProcDocumentRepository docRepository,
            IProcDocumentApprovalRepository procDocApprovalRepository,
            IJobTypeDocumentRepository configRepository,
            RoleManager<Role> roleManager
        )
        {
            _docRepository = docRepository;
            _procDocApprovalRepository = procDocApprovalRepository;
            _configRepository = configRepository;
            _roleManager = roleManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CanApproveProcDocumentRequirement requirement,
            ApproveDocContext resource
        ) {
            if (context.User.IsInRole("Admin")) {
                context.Succeed(requirement);
                return;
            }

            var doc = await _docRepository.GetByIdAsync(resource.ProcDocumentId);
            if (doc is null || doc.Procurement is null || doc.Procurement.JobTypeId is null) {
                return;
            }

            var config = await _configRepository.FindByJobTypeAndDocTypeAsync(
                doc.Procurement.JobTypeId,
                doc.DocumentTypeId
            );
            if (config is null || config.DocumentApprovals is null || config.DocumentApprovals.Count == 0)
                return;

            const decimal ThreshHoldVP = 300_000_000m;
            bool needVP = resource.TotalPenawaran > ThreshHoldVP;

            var configured = config
                .DocumentApprovals
                .OrderBy(a => a.Level)
                .ThenBy(a => a.SequenceOrder)
                .ToList();

            if (!needVP) {
                configured = configured.Where(approval => approval.Role != null && approval.Role.Name != "Vice President").ToList();
            }

            if (configured.Count == 0)
                return;

            var allApprovals = await _procDocApprovalRepository.GetByProcDocumentIdAsync(doc.ProcDocumentId);
            var approvedRoleIds = allApprovals.Where(approval => approval.Status == "Approved").Select(approval => approval.RoleId).ToHashSet();

            if (allApprovals.Any(approval => approval.Status == "Rejected"))
                return;

            var next = configured.FirstOrDefault(c => !approvedRoleIds.Contains(c.RoleId));
            if (next is null)
                return;

            var role = await _roleManager.FindByIdAsync(next.RoleId);
            if (role is null)
                return;

            if (context.User.IsInRole(role.Name!))
                context.Succeed(requirement);
        }


    }
}
