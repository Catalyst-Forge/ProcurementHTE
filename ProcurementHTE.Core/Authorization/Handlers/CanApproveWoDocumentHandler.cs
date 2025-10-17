using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Authorization.Requirements;
using ProcurementHTE.Core.Authorization.Resources;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Authorization.Handlers {
    public sealed class CanApproveWoDocumentHandler
        : AuthorizationHandler<CanApproveWoDocumentRequirement, ApproveDocContext> {
        private readonly IWoDocumentsRepository _docRepository;
        private readonly IWoDocumentApprovalsRepository _woDocApprovalRepository;
        private readonly IWoTypeDocumentsRepository _configRepository;
        private readonly RoleManager<Role> _roleManager;

        public CanApproveWoDocumentHandler(
            IWoDocumentsRepository docRepository,
            IWoDocumentApprovalsRepository woDocApprovalRepository,
            IWoTypeDocumentsRepository configRepository,
            RoleManager<Role> roleManager) {
            _docRepository = docRepository;
            _woDocApprovalRepository = woDocApprovalRepository;
            _configRepository = configRepository;
            _roleManager = roleManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CanApproveWoDocumentRequirement requirement,
            ApproveDocContext resource) {
            if (context.User.IsInRole("Admin")) {
                context.Succeed(requirement);
                return;
            }

            var doc = await _docRepository.GetByIdWithWorkOrderAsync(resource.WoDocumentId);
            if (doc is null || doc.WorkOrder is null || doc.WorkOrder.WoTypeId is null) {
                return;
            }

            var config = await _configRepository.GetForWoTypeAndDocTypeAsync(
                doc.WorkOrder.WoTypeId.Value,
                doc.DocumentTypeId
            );
            if (config is null)
                return;

            var configured = config
                .DocumentApprovals.OrderBy(a => a.Level)
                .ThenBy(a => a.SequenceOrder)
                .ToList();
            if (configured.Count == 0)
                return;

            var approved = (await _woDocApprovalRepository.GetApprovedAsync(doc.Id))
                .Select(r => r.RoleId)
                .ToHashSet();

            var next = configured.FirstOrDefault(c => !approved.Contains(c.RoleId));
            if (next is null)
                return;

            var role = await _roleManager.FindByNameAsync(next.RoleId);
            if (role is null)
                return;

            if (context.User.IsInRole(role.Name!))
                context.Succeed(requirement);
        }
    }
}
