using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public class HtmlTokenReplacer : IHtmlTokenReplacer
    {
        private static readonly CultureInfo Id = new("id-ID");
        private readonly IProcurementRepository _woRepo;
        private readonly IProfitLossRepository _pnlRepo;
        private readonly IVendorRepository _vendorRepo;

        public HtmlTokenReplacer(
            IProcurementRepository woRepo,
            IProfitLossRepository pnlRepo,
            IVendorRepository vendorRepo
        )
        {
            _woRepo = woRepo;
            _pnlRepo = pnlRepo;
            _vendorRepo = vendorRepo;
        }

        public async Task<string> ReplaceTokensAsync(
            string template,
            Procurement procurement,
            CancellationToken ct = default
        )
        {
            // Load full Procurement dengan relasi
            var wo =
                await _woRepo.GetByIdAsync(procurement.ProcurementId)
                ?? throw new InvalidOperationException("Procurement tidak ditemukan");

            var html = template;

            string FormatDate(DateTime? value) => value?.ToString("dd MMMM yyyy", Id) ?? "-";
            string FormatDecimal(decimal? value, string format = "N0") =>
                value.HasValue ? value.Value.ToString(format, Id) : "-";
            string GetUserName(User? user) =>
                user?.FullName ?? user?.UserName ?? user?.Email ?? "-";

            var jobTypeName = wo.JobTypeConfig?.TypeName ?? wo.JobType ?? "-";

            // Basic Procurement fields (reuse legacy token names for compatibility)
            html = ReplaceToken(html, "ProcNum", wo.ProcNum);
            html = ReplaceToken(html, "Description", wo.JobName);
            html = ReplaceToken(html, "Note", wo.Note);
            html = ReplaceToken(html, "ProcurementType", jobTypeName);
            html = ReplaceToken(html, "SpkNumber", wo.SpkNumber);
            html = ReplaceToken(html, "DateLetter", FormatDate(wo.StartDate));
            html = ReplaceToken(html, "From", GetUserName(wo.PicOpsUser));
            html = ReplaceToken(html, "To", GetUserName(wo.ManagerSignerUser));
            html = ReplaceToken(html, "ProcurementLetter", wo.LtcName);
            html = ReplaceToken(html, "WBS", wo.ProjectCode);
            html = ReplaceToken(html, "GlAccount", wo.ContractType);
            html = ReplaceToken(html, "DateRequired", FormatDate(wo.EndDate));
            html = ReplaceToken(html, "Requester", GetUserName(wo.User));
            html = ReplaceToken(html, "Approved", GetUserName(wo.ManagerSignerUser));
            html = ReplaceToken(html, "CreatedAt", FormatDate(wo.CreatedAt));
            html = ReplaceToken(html, "UpdatedAt", FormatDate(wo.UpdatedAt));
            html = ReplaceToken(html, "CompletedAt", FormatDate(wo.CompletedAt));

            // Additional new fields
            html = ReplaceToken(html, "ProjectRegion", wo.ProjectRegion);
            html = ReplaceToken(html, "DistanceKm", FormatDecimal(wo.DistanceKm, "N2"));
            html = ReplaceToken(html, "AccrualAmount", FormatDecimal(wo.AccrualAmount));
            html = ReplaceToken(html, "RealizationAmount", FormatDecimal(wo.RealizationAmount));
            html = ReplaceToken(html, "PotentialAccrualDate", FormatDate(wo.PotentialAccrualDate));
            html = ReplaceToken(html, "SpmpNumber", wo.SpmpNumber);
            html = ReplaceToken(html, "MemoNumber", wo.MemoNumber);
            html = ReplaceToken(html, "OeNumber", wo.OeNumber);
            html = ReplaceToken(html, "SelectedVendorName", wo.SelectedVendorName);
            html = ReplaceToken(html, "VendorSphNumber", wo.VendorSphNumber);
            html = ReplaceToken(html, "RaNumber", wo.RaNumber);
            html = ReplaceToken(html, "LtcName", wo.LtcName);

            // Relations
            html = ReplaceToken(html, "JobTypeName", jobTypeName);
            html = ReplaceToken(html, "StatusName", wo.Status?.StatusName);
            html = ReplaceToken(html, "UserName", wo.User?.UserName);

            // ProcDetails - Generate table rows
            if (wo.ProcDetails != null && wo.ProcDetails.Count != 0)
            {
                var detailsHtml = GenerateDetailsTable(wo.ProcDetails);
                html = html.Replace("{{ProcDetailsTable}}", detailsHtml);
            }
            else
            {
                html = html.Replace(
                    "{{ProcDetailsTable}}",
                    "<tr><td colspan='4' class='text-center'>Tidak ada detail</td></tr>"
                );
            }

            // Profit & Loss data
            var pnl = await _pnlRepo.GetLatestByProcurementIdAsync(wo.ProcurementId);
            if (pnl != null)
            {
                var items = pnl.Items ?? new List<ProfitLossItem>();

                string FormatInt(int value) => value.ToString("N0", Id);
                decimal Sum(Func<ProfitLossItem, decimal> selector) => items.Sum(selector);

                html = ReplaceToken(html, "TarifAwal", FormatDecimal(Sum(i => i.TarifAwal)));
                html = ReplaceToken(html, "TarifAdd", FormatDecimal(Sum(i => i.TarifAdd)));
                html = ReplaceToken(html, "KmPer25", FormatInt(items.Sum(i => i.KmPer25)));
                html = ReplaceToken(html, "OperatorCost", FormatDecimal(Sum(i => i.OperatorCost)));
                html = ReplaceToken(html, "Revenue", FormatDecimal(Sum(i => i.Revenue)));
                html = ReplaceToken(
                    html,
                    "SelectedVendorFinalOffer",
                    FormatDecimal(pnl.SelectedVendorFinalOffer)
                );
                html = ReplaceToken(html, "Profit", FormatDecimal(pnl.Profit));
                html = ReplaceToken(html, "ProfitPercent", pnl.ProfitPercent.ToString("N2", Id));

                if (!string.IsNullOrEmpty(pnl.SelectedVendorId))
                {
                    var vendor = await _vendorRepo.GetByIdAsync(pnl.SelectedVendorId);
                    if (vendor != null)
                    {
                        html = ReplaceToken(html, "SelectedVendorName", vendor.VendorName);
                        html = ReplaceToken(html, "SelectedVendorNPWP", vendor.NPWP);
                        html = ReplaceToken(html, "SelectedVendorAddress", vendor.Address);
                        html = ReplaceToken(html, "SelectedVendorCity", vendor.City);
                        html = ReplaceToken(html, "SelectedVendorProvince", vendor.Province);
                        html = ReplaceToken(html, "SelectedVendorEmail", vendor.Email);
                    }
                    else
                    {
                        html = ReplaceToken(html, "SelectedVendorName", "-");
                        html = ReplaceToken(html, "SelectedVendorNPWP", "-");
                        html = ReplaceToken(html, "SelectedVendorAddress", "-");
                        html = ReplaceToken(html, "SelectedVendorCity", "-");
                        html = ReplaceToken(html, "SelectedVendorProvince", "-");
                        html = ReplaceToken(html, "SelectedVendorEmail", "-");
                    }
                }
                else
                {
                    html = ReplaceToken(html, "SelectedVendorName", "-");
                    html = ReplaceToken(html, "SelectedVendorNPWP", "-");
                    html = ReplaceToken(html, "SelectedVendorAddress", "-");
                    html = ReplaceToken(html, "SelectedVendorCity", "-");
                    html = ReplaceToken(html, "SelectedVendorProvince", "-");
                    html = ReplaceToken(html, "SelectedVendorEmail", "-");
                }
            }
            else
            {
                html = ReplaceToken(html, "TarifAwal", "-");
                html = ReplaceToken(html, "TarifAdd", "-");
                html = ReplaceToken(html, "KmPer25", "-");
                html = ReplaceToken(html, "OperatorCost", "-");
                html = ReplaceToken(html, "Revenue", "-");
                html = ReplaceToken(html, "SelectedVendorFinalOffer", "-");
                html = ReplaceToken(html, "Profit", "-");
                html = ReplaceToken(html, "ProfitPercent", "-");
                html = ReplaceToken(html, "SelectedVendorName", "-");
                html = ReplaceToken(html, "SelectedVendorNPWP", "-");
                html = ReplaceToken(html, "SelectedVendorAddress", "-");
                html = ReplaceToken(html, "SelectedVendorCity", "-");
                html = ReplaceToken(html, "SelectedVendorProvince", "-");
                html = ReplaceToken(html, "SelectedVendorEmail", "-");
            }

            // Current date/time untuk footer
            html = ReplaceToken(html, "CurrentDate", DateTime.Now.ToString("dd MMMM yyyy"));
            html = ReplaceToken(
                html,
                "CurrentDateTime",
                DateTime.Now.ToString("dd MMMM yyyy HH:mm")
            );
            html = ReplaceToken(html, "CurrentYear", DateTime.Now.Year.ToString());

            return html;
        }

        private static string ReplaceToken(string html, string tokenName, string? value)
        {
            return html.Replace($"{{{{{tokenName}}}}}", value ?? "-");
        }

        private static string GenerateDetailsTable(ICollection<ProcDetail> details)
        {
            var sb = new StringBuilder();
            var no = 1;

            foreach (var detail in details)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"  <td class='text-center'>{no++}</td>");
                sb.AppendLine($"  <td>{detail.ItemName ?? "-"}</td>");
                sb.AppendLine($"  <td class='text-center'>{detail.Quantity ?? 0}</td>");
                sb.AppendLine($"  <td class='text-center'>{detail.Unit ?? "-"}</td>");
                sb.AppendLine("</tr>");
            }

            return sb.ToString();
        }
    }
}
