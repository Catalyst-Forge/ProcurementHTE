using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Interfaces
{
    public interface IHtmlTokenReplacer
    {
        Task<string> ReplaceTokensAsync(
            string template,
            Procurement procurement,
            CancellationToken ct = default
        );
    }
}
