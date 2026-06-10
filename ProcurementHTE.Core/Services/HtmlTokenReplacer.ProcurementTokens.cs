using ProcurementHTE.Core.Utils;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private async Task ApplyProcurementTokensAsync(
            HtmlTokenReplacementContext context,
            ProcurementUserNames names,
            decimal contractTotal
        )
        {
            await ApplyApprovalWorkflowTokensAsync(context, names, contractTotal);
            ApplyBasicProcurementTokens(context, names);
            var roleLabels = ApplyRoleLabelTokens(context);
            ApplyProcurementDateAndReferenceTokens(context);
            await ApplyConditionalApprovalTokensAsync(context, names, roleLabels);
            ApplyProcurementTableTokens(context);
        }

        private async Task ApplyApprovalWorkflowTokensAsync(
            HtmlTokenReplacementContext context,
            ProcurementUserNames names,
            decimal contractTotal
        )
        {
            if (contractTotal < 500_000_000m)
            {
                context.Replace("SubmitterName", names.AnalystHteName);
                context.Replace("Approver1Name", names.AssistantManagerName);
                context.Replace("Approver2Name", names.ManagerName);
                return;
            }

            context.Replace("SubmitterName", names.AssistantManagerName);
            context.Replace("Approver1Name", names.ManagerName);
            context.Replace("Approver2Name", await ResolveFirstUserNameByRoleAsync("Vice President"));
        }

        private static void ApplyBasicProcurementTokens(
            HtmlTokenReplacementContext context,
            ProcurementUserNames names
        )
        {
            var proc = context.Procurement;
            context.Replace("ProcNum", proc.ProcNum);
            context.Replace("JobName", proc.JobName);
            context.Replace("Note", proc.Note);
            context.Replace("Wonum", proc.Wonum);
            context.Replace("DocumentDate", FormatDate(proc.DocumentDate));
            context.Replace("StartDate", FormatDate(proc.StartDate));
            context.Replace("EndDate", FormatDate(proc.EndDate));
            context.Replace("PicOpsUserId", names.PicOpsName);
            context.Replace("AnalystHteUser", names.AnalystHteName);
            context.Replace("AsstManagerUserId", names.AssistantManagerName);
            context.Replace("ManagerUserId", names.ManagerName);
            context.Replace("LtcName", proc.LtcName);
            context.Replace("ProjectCode", proc.ProjectCode);
            context.Replace("ContractType", proc.ContractType.ToString());
            context.Replace("User", GetUserName(proc.User));
            context.Replace("CreatedAt", FormatDate(proc.CreatedAt));
            context.Replace("UpdatedAt", FormatDate(proc.UpdatedAt));
            context.Replace("CompletedAt", FormatDate(proc.CompletedAt));
        }

        private static ProcurementRoleLabels ApplyRoleLabelTokens(
            HtmlTokenReplacementContext context
        )
        {
            var proc = context.Procurement;
            var labels = new ProcurementRoleLabels(
                proc.AnalystHtePjs ? "Pjs. Analyst HTE & LTS" : "Analyst HTE & LTS",
                proc.AssistantManagerPjs ? "Pjs. Assistant Manager HTE" : "Assistant Manager HTE",
                proc.ManagerPjs
                    ? "Pjs. Manager Transport & Logistic"
                    : "Manager Transport & Logistic",
                proc.VicePresidentPjs ? "Pjs. Vice President" : "Vice President",
                proc.OperationDirectorPjs ? "Pjs. Operation Director" : "Operation Director",
                proc.PresidentDirectorPjs ? "Pjs. President Director" : "President Director"
            );

            context.Replace("AnalystHteRole", labels.AnalystHteRole);
            context.Replace("AsstManagerRole", labels.AssistantManagerRole);
            context.Replace("ManagerRole", labels.ManagerRole);
            context.Replace("VicePresidentRole", labels.VicePresidentRole);
            context.Replace("OperationDirectorRole", labels.OperationDirectorRole);
            context.Replace("PresidentDirectorRole", labels.PresidentDirectorRole);

            return labels;
        }

        private static void ApplyProcurementDateAndReferenceTokens(
            HtmlTokenReplacementContext context
        )
        {
            var proc = context.Procurement;
            context.Replace("RKSList", GenerateRKSJangkaWaktuList(proc));
            context.Replace("RKSSyaratList", GenerateRKSSyaratList(proc));
            context.Replace("ProcurementCategory", proc.ProcurementCategory.ToString());

            if (context.JobTypeName == "Moving" || context.JobTypeName == "Angkutan")
                context.Replace("ProcurementType", "PEKERJAAN");
            if (context.JobTypeName == "StandBy")
                context.Replace("ProcurementType", "SEWA");

            context.Replace("ProjectRegion", proc.ProjectRegion.ToString());
            context.Replace("PotentialAccrualDate", FormatDate(proc.PotentialAccrualDate));
            context.Replace(
                "TerbilangHari",
                proc.StartDate.ToTerbilangHari(proc.EndDate, includeUnitWord: true)
            );
            context.Replace(
                "TerbilangHariKata",
                proc.StartDate.ToTerbilangHari(proc.EndDate, includeUnitWord: false)
            );
            context.Replace(
                "JumlahHari",
                ((int)(proc.EndDate.Date - proc.StartDate.Date).TotalDays).ToString()
            );
            context.Replace("SpmpNumber", proc.SpmpNumber);
            context.Replace("MemoNumber", proc.MemoNumber);
            context.Replace(
                "MemoNumberPage2",
                int.TryParse(proc.MemoNumber, out var memoNumberInt)
                    ? (memoNumberInt + 1).ToString()
                    : "-"
            );
            context.Replace("OeNumber", proc.OeNumber);
            context.Replace("RaNumber", proc.RaNumber);
            context.Replace("LtcName", proc.LtcName);
            context.Replace("SpkNumber", proc.SpkNumber);
            context.Replace("JobTypeName", context.JobTypeName);
            context.Replace("StatusName", proc.Status?.StatusName);
            context.Replace("UserName", proc.User?.UserName);
        }
    }
}
