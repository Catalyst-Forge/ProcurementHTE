using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private async Task ApplyConditionalApprovalTokensAsync(
            HtmlTokenReplacementContext context,
            ProcurementUserNames names,
            ProcurementRoleLabels roleLabels
        )
        {
            var proc = context.Procurement;
            var docName = MapTemplateKeyToDocName(context.TemplateKey);
            var grandTotal = await GetCtAsync(proc.ProcurementId);
            var (submitRole, approveRole, submitUserId, approveUserId) =
                await ResolveConditionalRolesAsync(proc, docName, grandTotal);

            context.Replace("ConditionalSubmitRole", submitRole);
            context.Replace("ConditionalApproveRole", approveRole);

            var submitName = !string.IsNullOrWhiteSpace(submitUserId)
                ? await ResolveUserNameByIdAsync(submitUserId)
                : await ResolveUserNameByRoleWithProcDataAsync(
                    submitRole,
                    names.AnalystHteName,
                    names.AssistantManagerName,
                    names.ManagerName
                );
            var approveName = !string.IsNullOrWhiteSpace(approveUserId)
                ? await ResolveUserNameByIdAsync(approveUserId)
                : await ResolveUserNameByRoleWithProcDataAsync(
                    approveRole,
                    names.AnalystHteName,
                    names.AssistantManagerName,
                    names.ManagerName
                );

            context.Replace("ConditionalSubmitName", submitName);
            context.Replace("ConditionalApproveName", approveName);
            ApplyConditionalApproveCells(context, approveRole, approveName, grandTotal, roleLabels);
        }

        private static void ApplyConditionalApproveCells(
            HtmlTokenReplacementContext context,
            string approveRole,
            string approveName,
            decimal grandTotal,
            ProcurementRoleLabels roleLabels
        )
        {
            var isPnlDoc = MapTemplateKeyToDocName(context.TemplateKey) == "Profit & Loss";
            var needExtraApprove =
                !string.IsNullOrWhiteSpace(approveRole)
                && approveRole != "-"
                && (!isPnlDoc || grandTotal >= 500_000_000m);

            var approveRoleWithPjs = approveRole switch
            {
                "Vice President" => roleLabels.VicePresidentRole,
                "Operation Director" => roleLabels.OperationDirectorRole,
                "President Director" => roleLabels.PresidentDirectorRole,
                _ => approveRole,
            };

            context.Replace(
                "ConditionalApproveHeaderCell",
                needExtraApprove ? "<td style=\"width: 25%\">Disetujui Oleh</td>" : string.Empty
            );
            context.Replace(
                "ConditionalApproveBlankCell",
                needExtraApprove ? "<td class=\"signature-content\"></td>" : string.Empty
            );
            context.Replace(
                "ConditionalApproveRoleCell",
                needExtraApprove ? $"<td>{approveRoleWithPjs}</td>" : string.Empty
            );
            context.Replace(
                "ConditionalApproveNameCell",
                needExtraApprove ? $"<td>{approveName}</td>" : string.Empty
            );
        }

        private async Task<(
            string SubmitRole,
            string ApproveRole,
            string? SubmitUserId,
            string? ApproveUserId
        )> ResolveConditionalRolesAsync(Procurement procurement, string? docName, decimal ct)
        {
            if (string.IsNullOrWhiteSpace(docName))
                return ("-", "-", null, null);

            var rules = await _ruleRepo.GetActiveByDocNameAsync(
                docName,
                procurement.JobTypeId,
                procurement.ProcurementCategory
            );
            var hit = rules.FirstOrDefault(r => ct >= r.MinAmount && ct <= r.MaxAmount);
            if (hit == null)
                return ("-", "-", null, null);

            return (
                await ResolveRoleNameAsync(hit.SubmitterRoleId),
                await ResolveRoleNameAsync(hit.ApproverRoleId),
                hit.SubmitterUserId,
                hit.ApproverUserId
            );
        }

        private async Task<decimal> GetCtAsync(string procurementId)
        {
            var pnl = await _pnlRepo.GetLatestByProcurementIdAsync(procurementId);
            if (pnl == null)
                return 0m;

            if (pnl.SelectedVendorFinalOffer > 0m)
                return pnl.SelectedVendorFinalOffer;

            if (pnl.AccrualAmount.HasValue && pnl.AccrualAmount.Value > 0m)
                return pnl.AccrualAmount.Value;

            var revenueTotal = pnl.Items?.Sum(i => i.Revenue) ?? 0m;
            return revenueTotal > 0m ? revenueTotal : 0m;
        }

        private async Task<string> ResolveRoleNameAsync(string? roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
                return "-";

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
                return roleId;

            return !string.IsNullOrWhiteSpace(role.Name) ? role.Name! : roleId;
        }

        private static string? MapTemplateKeyToDocName(string? templateKey)
        {
            if (string.IsNullOrWhiteSpace(templateKey))
                return null;

            return templateKey switch
            {
                "ProfitLoss" => "Profit & Loss",
                "OwnerEstimate" => "Owner Estimate (OE)",
                "RKS" => "Rencana Kerja dan Syarat-Syarat (RKS)",
                "Justifikasi" => "Justifikasi",
                _ => templateKey,
            };
        }
    }
}
