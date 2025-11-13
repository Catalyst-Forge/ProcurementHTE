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
        private readonly IWorkOrderRepository _woRepo;
        private readonly IProfitLossRepository _pnlRepo;
        private readonly IVendorRepository _vendorRepo;

        public HtmlTokenReplacer(
            IWorkOrderRepository woRepo,
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
            WorkOrder workOrder,
            CancellationToken ct = default
        )
        {
            // Load full WorkOrder dengan relasi
            var wo =
                await _woRepo.GetByIdAsync(workOrder.WorkOrderId)
                ?? throw new InvalidOperationException("Work Order tidak ditemukan");

            var html = template;

            // Basic WorkOrder fields
            html = ReplaceToken(html, "WoNum", wo.WoNum);
            html = ReplaceToken(html, "Description", wo.Description);
            html = ReplaceToken(html, "Note", wo.Note);
            html = ReplaceToken(html, "ProcurementType", wo.ProcurementType.ToString());
            html = ReplaceToken(html, "WoNumLetter", wo.WoNumLetter);
            html = ReplaceToken(html, "DateLetter", wo.DateLetter.ToString("dd MMMM yyyy"));
            html = ReplaceToken(html, "From", wo.From);
            html = ReplaceToken(html, "To", wo.To);
            html = ReplaceToken(html, "WorkOrderLetter", wo.WorkOrderLetter);
            html = ReplaceToken(html, "WBS", wo.WBS);
            html = ReplaceToken(html, "GlAccount", wo.GlAccount);
            html = ReplaceToken(html, "DateRequired", wo.DateRequired.ToString("dd MMMM yyyy"));
            html = ReplaceToken(html, "Requester", wo.Requester);
            html = ReplaceToken(html, "Approved", wo.Approved);
            html = ReplaceToken(html, "CreatedAt", wo.CreatedAt.ToString("dd MMMM yyyy"));
            html = ReplaceToken(html, "UpdatedAt", wo.UpdatedAt?.ToString("dd MMMM yyyy"));
            html = ReplaceToken(html, "CompletedAt", wo.CompletedAt?.ToString("dd MMMM yyyy"));

            // Relations
            html = ReplaceToken(html, "WoTypeName", wo.WoType?.TypeName);
            html = ReplaceToken(html, "StatusName", wo.Status?.StatusName);
            html = ReplaceToken(html, "UserName", wo.User?.UserName);

            // WoDetails - Generate table rows
            if (wo.WoDetails != null && wo.WoDetails.Count != 0)
            {
                var detailsHtml = GenerateDetailsTable(wo.WoDetails);
                html = html.Replace("{{WoDetailsTable}}", detailsHtml);
            }
            else
            {
                html = html.Replace(
                    "{{WoDetailsTable}}",
                    "<tr><td colspan='4' class='text-center'>Tidak ada detail</td></tr>"
                );
            }

            // Profit & Loss data
            var pnl = await _pnlRepo.GetLatestByWorkOrderIdAsync(wo.WorkOrderId);
            if (pnl != null)
            {
                var items = pnl.Items ?? new List<ProfitLossItem>();

                string FormatDecimal(decimal value, string format = "N0") =>
                    value.ToString(format, Id);

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

        private static string GenerateDetailsTable(ICollection<WoDetail> details)
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
