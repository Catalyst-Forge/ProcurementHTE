using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using System.Text;

namespace ProcurementHTE.Core.Services {
    public class HtmlTokenReplacer : IHtmlTokenReplacer {
        private readonly IWorkOrderRepository _woRepo;
        private readonly IProfitLossRepository _pnlRepo;
        private readonly IVendorRepository _vendorRepo;
        private readonly ILogger<HtmlTokenReplacer> _logger;

        public HtmlTokenReplacer(
            IWorkOrderRepository woRepo,
            IProfitLossRepository pnlRepo,
            IVendorRepository vendorRepo,
            ILogger<HtmlTokenReplacer> logger) {
            _woRepo = woRepo;
            _pnlRepo = pnlRepo;
            _vendorRepo = vendorRepo;
            _logger = logger;
        }

        public async Task<string> ReplaceTokensAsync(string template, WorkOrder workOrder, CancellationToken ct = default) {
            // Load full WorkOrder dengan relasi
            var wo = await _woRepo.GetByIdAsync(workOrder.WorkOrderId)
                     ?? throw new InvalidOperationException("Work Order tidak ditemukan");

            var html = template;

            // Basic WorkOrder fields
            html = ReplaceToken(html, "WoNum", wo.WoNum);
            html = ReplaceToken(html, "Description", wo.Description);
            html = ReplaceToken(html, "Note", wo.Note);
            html = ReplaceToken(html, "ProcurementType", wo.ProcurementType.ToString());
            html = ReplaceToken(html, "WoNumLetter", wo.WoNumLetter);
            html = ReplaceToken(html, "DateLetter", wo.DateLetter?.ToString("dd MMMM yyyy"));
            html = ReplaceToken(html, "FromLocation", wo.FromLocation);
            html = ReplaceToken(html, "Destination", wo.Destination);
            html = ReplaceToken(html, "WorkOrderLetter", wo.WorkOrderLetter);
            html = ReplaceToken(html, "WBS", wo.WBS);
            html = ReplaceToken(html, "GlAccount", wo.GlAccount);
            html = ReplaceToken(html, "DateRequired", wo.DateRequired?.ToString("dd MMMM yyyy"));
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
            if (wo.WoDetails != null && wo.WoDetails.Any()) {
                var detailsHtml = GenerateDetailsTable(wo.WoDetails);
                html = html.Replace("{{WoDetailsTable}}", detailsHtml);
            } else {
                html = html.Replace("{{WoDetailsTable}}", "<tr><td colspan='4' class='text-center'>Tidak ada detail</td></tr>");
            }

            // Profit & Loss data
            var pnl = await _pnlRepo.GetLatestByWorkOrderIdAsync(wo.WorkOrderId);
            if (pnl != null) {
                html = ReplaceToken(html, "TarifAwal", pnl.TarifAwal.ToString("N0"));
                html = ReplaceToken(html, "TarifAdd", pnl.TarifAdd.ToString("N0"));
                html = ReplaceToken(html, "KmPer25", pnl.KmPer25.ToString());
                html = ReplaceToken(html, "OperatorCost", pnl.OperatorCost.ToString("N0"));
                html = ReplaceToken(html, "Revenue", pnl.Revenue.ToString("N0"));
                html = ReplaceToken(html, "SelectedVendorFinalOffer", pnl.SelectedVendorFinalOffer.ToString("N0"));
                html = ReplaceToken(html, "Profit", pnl.Profit.ToString("N0"));
                html = ReplaceToken(html, "ProfitPercent", pnl.ProfitPercent.ToString("N2"));

                // Selected Vendor
                if (!string.IsNullOrEmpty(pnl.SelectedVendorId)) {
                    var vendor = await _vendorRepo.GetByIdAsync(pnl.SelectedVendorId);
                    if (vendor != null) {
                        html = ReplaceToken(html, "SelectedVendorName", vendor.VendorName);
                        html = ReplaceToken(html, "SelectedVendorNPWP", vendor.NPWP);
                        html = ReplaceToken(html, "SelectedVendorAddress", vendor.Address);
                        html = ReplaceToken(html, "SelectedVendorCity", vendor.City);
                        html = ReplaceToken(html, "SelectedVendorProvince", vendor.Province);
                        html = ReplaceToken(html, "SelectedVendorEmail", vendor.Email);
                    }
                }
            }

            // Current date/time untuk footer
            html = ReplaceToken(html, "CurrentDate", DateTime.Now.ToString("dd MMMM yyyy"));
            html = ReplaceToken(html, "CurrentDateTime", DateTime.Now.ToString("dd MMMM yyyy HH:mm"));
            html = ReplaceToken(html, "CurrentYear", DateTime.Now.Year.ToString());

            return html;
        }

        private string ReplaceToken(string html, string tokenName, string? value) {
            return html.Replace($"{{{{{tokenName}}}}}", value ?? "-");
        }

        private string GenerateDetailsTable(ICollection<WoDetail> details) {
            var sb = new StringBuilder();
            var no = 1;

            foreach (var detail in details) {
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
