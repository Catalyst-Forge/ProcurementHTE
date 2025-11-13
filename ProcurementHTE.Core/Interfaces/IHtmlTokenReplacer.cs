using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IHtmlTokenReplacer
    {
        Task<string> ReplaceTokensAsync(
            string template,
            WorkOrder workOrder,
            CancellationToken ct = default
        );
    }
}
