using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ProcurementHTE.Core.Authorization.Requirements;
using ProcurementHTE.Core.Authorization.Resources;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Authorization.Handlers
{
    public sealed class CanApproveWoDocumentHandler
        : AuthorizationHandler<CanApproveWoDocumentRequirement, ApproveDocContext>
    {
        private readonly IWoDocumentRepository _docRepository;
        private readonly IWoDocumentApprovalRepository _woDocApprovalRepository;
        private readonly IWoTypeDocumentRepository _configRepository;
        private readonly RoleManager<Role> _roleManager;

        public CanApproveWoDocumentHandler(
            IWoDocumentRepository docRepository,
            IWoDocumentApprovalRepository woDocApprovalRepository,
            IWoTypeDocumentRepository configRepository,
            RoleManager<Role> roleManager
        )
        {
            _docRepository = docRepository;
            _woDocApprovalRepository = woDocApprovalRepository;
            _configRepository = configRepository;
            _roleManager = roleManager;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanApproveWoDocumentRequirement requirement, ApproveDocContext resource)
        {
            throw new NotImplementedException();
        }

        //protected override async Task HandleRequirementAsync(
        //    AuthorizationHandlerContext context,
        //    CanApproveWoDocumentRequirement requirement,
        //    ApproveDocContext resource
        //)
        //{
        //    if (context.User.IsInRole("Admin"))
        //    {
        //        context.Succeed(requirement);
        //        return;
        //    }

        //    var doc = await _docRepository.GetByIdWithWorkOrderAsync(resource.WoDocumentId);
        //    if (doc is null || doc.WorkOrder is null || doc.WorkOrder.WoTypeId is null)
        //    {
        //        return;
        //    }

        //    var config = await _configRepository.FindByWoTypeAndDocTypeAsync(
        //        doc.WorkOrder.WoTypeId,
        //        doc.DocumentTypeId
        //    );
        //    if (config is null)
        //        return;

        //    var configured = config
        //        .DocumentApprovals.OrderBy(a => a.Level)
        //        .ThenBy(a => a.SequenceOrder)
        //        .ToList();
        //    if (configured.Count == 0)
        //        return;

        //    var approved = (await _woDocApprovalRepository.GetApprovedByWoDocumentIdAsync(doc.WoDocumentId))
        //        .Select(r => r.RoleId)
        //        .ToHashSet();

        //    var next = configured.FirstOrDefault(c => !approved.Contains(c.RoleId));
        //    if (next is null)
        //        return;

        //    var role = await _roleManager.FindByNameAsync(next.RoleId);
        //    if (role is null)
        //        return;

        //    if (context.User.IsInRole(role.Name!))
        //        context.Succeed(requirement);
        //}

    }
}
