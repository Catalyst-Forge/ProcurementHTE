using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    internal sealed class HtmlTokenReplacementContext
    {
        public HtmlTokenReplacementContext(
            string template,
            Procurement procurement,
            string? templateKey
        )
        {
            Html = template;
            Procurement = procurement;
            TemplateKey = templateKey;
            JobTypeName = procurement.JobType?.TypeName;
        }

        public string Html { get; set; }
        public Procurement Procurement { get; }
        public string? TemplateKey { get; }
        public string? JobTypeName { get; }

        public void Replace(string tokenName, string? value)
        {
            Html = HtmlTokenFormatter.ReplaceToken(Html, tokenName, value);
        }
    }

    internal sealed record ProcurementUserNames(
        string PicOpsName,
        string AnalystHteName,
        string AssistantManagerName,
        string ManagerName
    );

    internal sealed record ProcurementRoleLabels(
        string AnalystHteRole,
        string AssistantManagerRole,
        string ManagerRole,
        string VicePresidentRole,
        string OperationDirectorRole,
        string PresidentDirectorRole
    );
}
