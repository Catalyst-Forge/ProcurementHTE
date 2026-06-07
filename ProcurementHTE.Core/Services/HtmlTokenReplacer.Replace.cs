using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        public async Task<string> ReplaceTokensAsync(
            string template,
            Procurement procurement,
            CancellationToken ct = default,
            string? templateKey = null
        )
        {
            _ = ct;

            var proc =
                await _procRepo.GetByIdAsync(procurement.ProcurementId)
                ?? throw new InvalidOperationException("Procurement tidak ditemukan");

            var context = new HtmlTokenReplacementContext(template, proc, templateKey);
            var names = await ResolveProcurementUserNamesAsync(proc);
            var contractTotal = await GetCtAsync(proc.ProcurementId);

            await ApplyProcurementTokensAsync(context, names, contractTotal);

            decimal? accrualAmount = null;
            decimal? realizationAmount = null;
            var pnl = await _pnlRepo.GetLatestByProcurementIdAsync(proc.ProcurementId);

            if (pnl != null)
            {
                (accrualAmount, realizationAmount) = await ApplyProfitLossTokensAsync(
                    context,
                    pnl
                );
            }
            else
            {
                ApplyProfitLossFallbackTokens(context);
            }

            ApplyOfferDetailTokens(context, pnl);
            context.Replace("AccrualAmount", FormatDecimal(accrualAmount));
            context.Replace("RealizationAmount", FormatDecimal(realizationAmount));
            ApplyCurrentDateTokens(context);

            return context.Html;
        }

        private static void ApplyCurrentDateTokens(HtmlTokenReplacementContext context)
        {
            context.Replace("CurrentDate", DateTime.Now.ToString("dd MMMM yyyy", Id));
            context.Replace("CurrentDateTime", DateTime.Now.ToString("dd MMMM yyyy HH:mm", Id));
            context.Replace("CurrentYear", DateTime.Now.Year.ToString());
        }
    }
}
