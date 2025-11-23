using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Utils;

namespace ProcurementHTE.Core.Services
{
    public class HtmlTokenReplacer : IHtmlTokenReplacer
    {
        #region Construct

        private static readonly CultureInfo Id = new("id-ID");
        private readonly IProcurementRepository _procRepo;
        private readonly IProfitLossRepository _pnlRepo;
        private readonly IVendorRepository _vendorRepo;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<HtmlTokenReplacer> _logger;

        public HtmlTokenReplacer(
            IProcurementRepository procurementRepository,
            IProfitLossRepository pnlRepo,
            IVendorRepository vendorRepo,
            UserManager<User> userManager,
            ILogger<HtmlTokenReplacer> logger
        )
        {
            _procRepo = procurementRepository;
            _pnlRepo = pnlRepo;
            _vendorRepo = vendorRepo;
            _userManager = userManager;
            _logger = logger;
        }

        #endregion

        public async Task<string> ReplaceTokensAsync(
            string template,
            Procurement procurement,
            CancellationToken ct = default,
            string? templateKey = null
        )
        {
            var html = template;

            string FormatDate(DateTime? value) => value?.ToString("dd MMMM yyyy", Id) ?? "-";
            string FormatDecimal(decimal? value, string format = "N0") =>
                value.HasValue ? value.Value.ToString(format, Id) : "-";
            string FormatCurrency(decimal? value, string format = "C0") =>
                value.HasValue ? value.Value.ToString(format, Id) : "-";
            string GetUserName(User? user) =>
                user?.FullName ?? user?.UserName ?? user?.Email ?? "-";
            async Task<string> ResolveUserNameAsync(string? userId)
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return "-";

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return userId; // fallback: kalau benar2 gak ketemu, tampilkan id aja

                if (!string.IsNullOrWhiteSpace(user.FullName))
                    return user.FullName;

                if (!string.IsNullOrWhiteSpace(user.UserName))
                    return user.UserName;

                return user.Email ?? userId;
            }

            #region Procurements

            // Load full Procurement dengan relasi
            var proc =
                await _procRepo.GetByIdAsync(procurement.ProcurementId)
                ?? throw new InvalidOperationException("Procurement tidak ditemukan");

            var jobTypeName = proc.JobType?.TypeName;
            var picOpsName = await ResolveUserNameAsync(proc.PicOpsUserId);
            var asstMgrName = await ResolveUserNameAsync(proc.AssistantManagerUserId);
            var mgrName = await ResolveUserNameAsync(proc.ManagerUserId);

            // Basic Procurement fields (reuse legacy token names for compatibility)
            html = ReplaceToken(html, "ProcNum", proc.ProcNum);
            html = ReplaceToken(html, "JobName", proc.JobName);
            html = ReplaceToken(html, "Note", proc.Note);
            html = ReplaceToken(html, "SpkNumber", proc.SpkNumber);
            html = ReplaceToken(html, "Wonum", proc.Wonum);
            html = ReplaceToken(html, "StartDate", FormatDate(proc.StartDate));
            html = ReplaceToken(html, "EndDate", FormatDate(proc.EndDate));
            html = ReplaceToken(html, "PicOpsUserId", picOpsName);
            html = ReplaceToken(html, "AsstManagerUserId", asstMgrName);
            html = ReplaceToken(html, "ManagerUserId", mgrName);
            html = ReplaceToken(html, "LtcName", proc.LtcName);
            html = ReplaceToken(html, "ProjectCode", proc.ProjectCode);
            html = ReplaceToken(html, "ContractType", proc.ContractType.ToString());
            html = ReplaceToken(html, "User", GetUserName(proc.User));
            html = ReplaceToken(html, "CreatedAt", FormatDate(proc.CreatedAt));
            html = ReplaceToken(html, "UpdatedAt", FormatDate(proc.UpdatedAt));
            html = ReplaceToken(html, "CompletedAt", FormatDate(proc.CompletedAt));

            // Additional new fields dari Procurement
            html = ReplaceToken(html, "ProjectRegion", proc.ProjectRegion.ToString());
            html = ReplaceToken(
                html,
                "PotentialAccrualDate",
                FormatDate(proc.PotentialAccrualDate)
            );
            html = ReplaceToken(html, "SpmpNumber", proc.SpmpNumber);
            html = ReplaceToken(html, "MemoNumber", proc.MemoNumber);
            html = ReplaceToken(html, "OeNumber", proc.OeNumber);
            html = ReplaceToken(html, "RaNumber", proc.RaNumber);
            html = ReplaceToken(html, "LtcName", proc.LtcName);

            // Relations
            html = ReplaceToken(html, "JobTypeName", jobTypeName);
            html = ReplaceToken(html, "StatusName", proc.Status?.StatusName);
            html = ReplaceToken(html, "UserName", proc.User?.UserName);

            // ProcDetails - Generate table rows
            if (proc.ProcDetails != null && proc.ProcDetails.Count != 0)
            {
                var detailsHtml = GenerateDetailsTable(proc.ProcDetails);
                html = ReplaceToken(html, "ProcDetailsTable", detailsHtml);
            }
            else
            {
                html = ReplaceToken(
                    html,
                    "ProcDetailsTable",
                    "<tr><td colspan='4' class='text-center'>Tidak ada detail</td></tr>"
                );
            }

            // ProcOffers - used in BOQ
            if (proc.ProcOffers != null && proc.ProcOffers.Count != 0)
            {
                var firstOffer = proc.ProcOffers.First();
                html = ReplaceToken(html, "Items.ItemPenawaran", firstOffer.ItemPenawaran);
                html = ReplaceToken(html, "Items.Quantity", firstOffer.Qty.ToString("N0", Id));
                html = ReplaceToken(html, "Items.Unit", firstOffer.Unit);
                html = ReplaceToken(html, "ProcOffersTable", GenerateOffersTable(proc.ProcOffers));
                html = ReplaceToken(html, "ProcOffersList", GenerateOffersList(proc.ProcOffers));
            }
            else
            {
                html = ReplaceToken(html, "Items.ItemPenawaran", "-");
                html = ReplaceToken(html, "Items.Quantity", "-");
                html = ReplaceToken(html, "Items.Unit", "-");
                html = ReplaceToken(
                    html,
                    "ProcOffersTable",
                    "<tr><td colspan='4' class='text-center'>Tidak ada item penawaran</td></tr>"
                );
            }

            #endregion

            #region Profit & Loss

            decimal? accrualAmount = null;
            decimal? realizationAmount = null;
            var pnl = await _pnlRepo.GetLatestByProcurementIdAsync(proc.ProcurementId);
            if (pnl != null)
            {
                var items = pnl.Items?.ToList() ?? new List<ProfitLossItem>();

                decimal Sum(Func<ProfitLossItem, decimal> selector) => items.Sum(selector);

                var revenueTotal = Sum(i => i.Revenue);
                accrualAmount = pnl.AccrualAmount ?? revenueTotal;
                realizationAmount = pnl.RealizationAmount;

                // Field dokumen-level dari ProfitLoss
                html = ReplaceToken(
                    html,
                    "SelectedVendorFinalOffer",
                    FormatDecimal(pnl.SelectedVendorFinalOffer)
                );
                html = ReplaceToken(html, "Profit", FormatDecimal(pnl.Profit));
                html = ReplaceToken(html, "ProfitPercent", pnl.ProfitPercent.ToString("N2", Id));
                html = ReplaceToken(html, "Distance", FormatDecimal(pnl.Distance));
                html = ReplaceToken(html, "PnlCreatedAt", FormatDate(pnl.CreatedAt));
                html = ReplaceToken(html, "PnlUpdatedAt", FormatDate(pnl.UpdatedAt));
                html = ReplaceToken(html, "TotalRevenue", FormatCurrency(revenueTotal));
                html = ReplaceToken(html, "RevenueTerbilang", revenueTotal.ToTerbilangRupiah());
                html = ReplaceToken(html, "NoLetter", pnl.NoLetterSelectedVendor);

                // Aggregat berdasarkan ProfitLossItem
                if (pnl.Items != null && pnl.Items.Count != 0)
                {
                    var itemsHtml = GenerateItemsTable(pnl.Items, templateKey);
                    html = ReplaceToken(html, "PnlItemsTable", itemsHtml);
                }

                // Tabel penawaran vendor
                if (pnl.VendorOffers != null && pnl.VendorOffers.Count != 0)
                {
                    var vendorOfferHtml = GenerateOfferTable(pnl, proc);
                    html = ReplaceToken(html, "VendorOfferTable", vendorOfferHtml);
                }
                else
                {
                    html = ReplaceToken(
                        html,
                        "VendorOfferTable",
                        "<p class='text-center'>Tidak ada penawaran vendor</p>"
                    );
                }

                // Data vendor terpilih (dari SelectedVendorId di ProfitLoss)
                if (!string.IsNullOrEmpty(pnl.SelectedVendorId))
                {
                    var allVendor = await _vendorRepo.GetAllAsync();
                    var selectVendor = await _pnlRepo.GetSelectedVendorsAsync(pnl.ProcurementId);

                    var participatingVendors = selectVendor
                        .Select(row => allVendor.FirstOrDefault(v => v.VendorId == row.VendorId))
                        .Where(v => v != null)
                        .Cast<Vendor>()
                        .ToList();

                    var vendorCount = allVendor.Count();
                    var selectedVendorCount = participatingVendors.Count;
                    var vendorList = GenerateVendorNameList(participatingVendors);

                    html = ReplaceToken(html, "VendorList", vendorList);
                    html = ReplaceToken(
                        html,
                        "SelectedVendorCount",
                        selectedVendorCount.ToString()
                    );
                    html = ReplaceToken(html, "VendorCount", vendorCount.ToString());
                    html = ReplaceToken(
                        html,
                        "SelectedVendorCountTerbilang",
                        selectedVendorCount.ToTerbilang()
                    );

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

                // ====== Tabel Profit & Loss Estimate (summary hijau) ======
                var pnlEstimateHtml = GeneratePnlEstimateTable(pnl, proc);
                html = ReplaceToken(html, "PnlEstimateTable", pnlEstimateHtml);
            }
            else
            {
                // Tidak ada PNL untuk Procurement ini → isi dengan "-"
                html = ReplaceToken(html, "QuantityTotal", "-");
                html = ReplaceToken(html, "TarifAwal", "-");
                html = ReplaceToken(html, "TarifAdd", "-");
                html = ReplaceToken(html, "KmPer25", "-");
                html = ReplaceToken(html, "OperatorCost", "-");
                html = ReplaceToken(html, "Revenue", "-");
                html = ReplaceToken(html, "Distance", "-");
                html = ReplaceToken(html, "SelectedVendorFinalOffer", "-");
                html = ReplaceToken(html, "Profit", "-");
                html = ReplaceToken(html, "ProfitPercent", "-");
                html = ReplaceToken(html, "PnlCreatedAt", "-");
                html = ReplaceToken(html, "PnlUpdatedAt", "-");
                html = ReplaceToken(html, "SelectedVendorName", "-");
                html = ReplaceToken(html, "SelectedVendorNPWP", "-");
                html = ReplaceToken(html, "SelectedVendorAddress", "-");
                html = ReplaceToken(html, "SelectedVendorCity", "-");
                html = ReplaceToken(html, "SelectedVendorProvince", "-");
                html = ReplaceToken(html, "SelectedVendorEmail", "-");
            }

            //Accrual & realization amount (pakai fallback ke total Revenue / OperatorCost)
            html = ReplaceToken(html, "AccrualAmount", FormatDecimal(accrualAmount));
            html = ReplaceToken(html, "RealizationAmount", FormatDecimal(realizationAmount));

            // Current date/time untuk footer
            html = ReplaceToken(html, "CurrentDate", DateTime.Now.ToString("dd MMMM yyyy", Id));
            html = ReplaceToken(
                html,
                "CurrentDateTime",
                DateTime.Now.ToString("dd MMMM yyyy HH:mm", Id)
            );
            html = ReplaceToken(html, "CurrentYear", DateTime.Now.Year.ToString());

            return html;

            #endregion
        }

        #region Helper Method

        private static string ReplaceToken(string html, string tokenName, string? value)
        {
            var replacement = value ?? "-";
            var pattern = $"{{{{\\s*{Regex.Escape(tokenName)}\\s*}}}}";
            return Regex.Replace(
                html,
                pattern,
                _ => replacement,
                RegexOptions.CultureInvariant | RegexOptions.Multiline
            );
        }

        #endregion

        #region Generate Table Items

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

        private static string GenerateOffersTable(ICollection<ProcOffer> offers)
        {
            if (offers == null || offers.Count == 0)
                return "<tr><td colspan='4' class='text-center'>Tidak ada item penawaran</td></tr>";

            var sb = new StringBuilder();
            var no = 1;

            foreach (var offer in offers)
            {
                var qtyFormatted = offer.Qty.ToString("N0", Id);
                sb.AppendLine("<tr>");
                sb.AppendLine($"  <td class='text-center'>{no++}</td>");
                sb.AppendLine($"  <td>{offer.ItemPenawaran}</td>");
                sb.AppendLine($"  <td class='text-center'>{qtyFormatted}</td>");
                sb.AppendLine($"  <td class='text-center'>{offer.Unit}</td>");
                sb.AppendLine("</tr>");
            }

            return sb.ToString();
        }

        private static string GenerateOffersList(ICollection<ProcOffer> offers)
        {
            var sb = new StringBuilder();

            foreach (var offer in offers)
            {
                sb.AppendLine("<ol class='sub-list'>");
                sb.AppendLine(
                    $"  <li class='mb-1'>{offer.Qty} ({offer.Qty.ToTerbilang()}) {offer.Unit} {offer.ItemPenawaran}</li>"
                );
                sb.AppendLine("</ol>");
            }

            return sb.ToString();
        }

        private static string GenerateItemsTable(ICollection<ProfitLossItem> items, string? docType)
        {
            var sb = new StringBuilder();
            var no = 1;

            foreach (var item in items)
            {
                var price = item.TarifAwal + (item.TarifAdd * item.KmPer25);
                var revenue = price * item.Quantity;
                sb.AppendLine("<tr>");
                sb.AppendLine($"  <td class='text-center'>{no++}</td>");
                sb.AppendLine($"  <td>{item.ProcOffer.ItemPenawaran}</td>");
                sb.AppendLine($"  <td class='text-center'>{item.Quantity}</td>");
                if (docType == "OwnerEstimate")
                {
                    sb.AppendLine($"  <td class='text-center'>{item.ProcOffer.Unit}</td>");
                    sb.AppendLine($"  <td class='text-end'>{price.ToString("C0", Id)}</td>");
                }
                if (docType == "ProfitLoss")
                {
                    sb.AppendLine(
                        $"  <td class='text-end'>{item.TarifAwal.ToString("N0", Id)}</td>"
                    );
                    sb.AppendLine(
                        $"  <td class='text-end'>{item.TarifAdd.ToString("N0", Id)}</td>"
                    );
                    sb.AppendLine($"  <td class='text-center'>{item.KmPer25}</td>");
                    sb.AppendLine(
                        $"  <td class='text-end'>{item.OperatorCost.ToString("N0", Id)}</td>"
                    );
                }
                sb.AppendLine($"  <td class='text-end'>{revenue.ToString("C0", Id)}</td>");
                sb.AppendLine("</tr>");
            }

            return sb.ToString();
        }

        private static string GenerateVendorNameList(ICollection<Vendor> vendorList)
        {
            if (vendorList == null || vendorList.Count == 0)
            {
                return "<p class='text-muted mb-0'>Tidak ada vendor.</p>";
            }

            var sb = new StringBuilder();

            var items = vendorList
                .Select((v, i) => new { No = i + 1, Name = v.VendorName ?? "-" })
                .ToList();

            sb.AppendLine("<div class='vendor-list mb-3'>");

            foreach (var item in items)
            {
                sb.AppendLine("  <div class='vendor-item'>");
                sb.AppendLine($"    <div class='vendor-item-no'>{item.No}</div>");
                sb.AppendLine($"    <div class='vendor-item-name'>{item.Name}</div>");
                sb.AppendLine("  </div>");
            }

            sb.AppendLine("</div>");

            return sb.ToString();
        }

        private static string GenerateOfferTable(ProfitLoss pnl, Procurement proc)
        {
            if (pnl.VendorOffers == null || pnl.VendorOffers.Count == 0)
            {
                return "<p class='text-center'>Tidak ada penawaran vendor.</p>";
            }

            var pnlItems = pnl.Items ?? new List<ProfitLossItem>();
            var pnlItemsByOfferId = pnlItems.ToDictionary(i => i.ProcOfferId, i => i);
            var procOffers = proc.ProcOffers ?? new List<ProcOffer>();
            var procOffersById = procOffers.ToDictionary(o => o.ProcOfferId, o => o);

            var sb = new StringBuilder();

            // Group berdasarkan vendor
            var vendorGroups = pnl
                .VendorOffers.GroupBy(vo => vo.VendorId)
                .OrderBy(g => g.First().Vendor.VendorName); // urut per nama vendor

            foreach (var vendorGroup in vendorGroups)
            {
                var first = vendorGroup.First();
                var vendor = first.Vendor;
                var vendorName = vendor?.VendorName ?? "-";

                // Round yang dipakai vendor ini
                var rounds = vendorGroup.Select(vo => vo.Round).Distinct().OrderBy(r => r).ToList();

                if (rounds.Count == 0)
                    continue;

                var minRound = rounds.First(); // biasanya 1 = harga awal
                var maxRound = rounds.Last(); // round terbesar = nego terakhir
                var totalRoundCount = maxRound - minRound + 1;

                // Group per item (ProcOfferId) untuk vendor ini
                var itemGroups = vendorGroup
                    .GroupBy(vo => vo.ProcOfferId)
                    .OrderBy(g => g.First().ProcOffer.ItemPenawaran);

                decimal grandTotal = 0m;

                // HEADER TABEL VENDOR
                sb.AppendLine("<div class='mb-4'>");
                sb.AppendLine($"  <div class='fw-semibold mb-1'>{vendorName}</div>");
                sb.AppendLine(
                    "  <table class='table table-bordered table-sm align-middle border-black'>"
                );
                sb.AppendLine("    <thead>");
                sb.AppendLine("      <tr>");
                sb.AppendLine(
                    "        <th class='blue-header text-center' style='width: 1rem'>No</th>"
                );
                sb.AppendLine("        <th class='blue-header' style='width: 15rem'>Item</th>");
                sb.AppendLine(
                    "        <th class='blue-header text-center' style='width: 2rem'>Unit</th>"
                );
                sb.AppendLine(
                    "        <th class='blue-header text-center' style='width: 5rem'>Trip</th>"
                );
                sb.AppendLine("        <th class='blue-header text-center'>Harga awal</th>");

                // Kolom Harga Nego #1..#n (dinamis dari round)
                for (var r = minRound + 1; r <= maxRound; r++)
                {
                    var negoIndex = r - minRound;
                    sb.AppendLine(
                        $"        <th class='blue-header text-center'>Harga Nego #{negoIndex}</th>"
                    );
                }

                sb.AppendLine("        <th class='blue-header text-center'>Total</th>");
                sb.AppendLine("      </tr>");
                sb.AppendLine("    </thead>");
                sb.AppendLine("    <tbody>");

                var no = 1;

                foreach (var itemGroup in itemGroups)
                {
                    var procOfferId = itemGroup.Key;

                    // Ambil definisi item dari PNL atau dari Procurement
                    pnlItemsByOfferId.TryGetValue(procOfferId, out var pnlItem);
                    procOffersById.TryGetValue(procOfferId, out var baseOffer);

                    var itemName =
                        baseOffer?.ItemPenawaran ?? pnlItem?.ProcOffer?.ItemPenawaran ?? "-";

                    var qty = pnlItem?.Quantity ?? baseOffer?.Qty ?? 0;

                    // Trip & unit diambil dari penawaran vendor (asumsi: sama untuk semua round)
                    var firstOfferForItem = itemGroup.OrderBy(vo => vo.Round).First();

                    var unit = baseOffer?.Unit ?? "-";
                    var trip = firstOfferForItem.Trip; // property Trip di VendorOffer

                    // List harga per round (nullable), indeks 0 = round minRound (Harga awal)
                    var pricesPerRound = new decimal?[totalRoundCount];

                    foreach (var offer in itemGroup)
                    {
                        var index = offer.Round - minRound;
                        if (index < 0 || index >= totalRoundCount)
                            continue;

                        pricesPerRound[index] = offer.Price;
                    }

                    // Tentukan harga final:
                    //   - harga paling rendah
                    //   - jika ada beberapa round dengan harga sama, ambil round terbesar
                    decimal? finalPrice = null;
                    int? finalRound = null;

                    foreach (var offer in itemGroup)
                    {
                        var price = offer.Price;
                        if (price <= 0)
                            continue;

                        if (
                            finalPrice == null
                            || price < finalPrice
                            || (
                                price == finalPrice
                                && (finalRound == null || offer.Round > finalRound)
                            )
                        )
                        {
                            finalPrice = price;
                            finalRound = offer.Round;
                        }
                    }

                    decimal? total = null;
                    if (finalPrice.HasValue && qty > 0 && trip > 0)
                    {
                        total = finalPrice.Value * qty * trip;
                        grandTotal += total.Value;
                    }

                    // RENDER BARIS ITEM
                    sb.AppendLine("      <tr>");
                    sb.AppendLine($"        <td class='text-center'>{no++}</td>");
                    sb.AppendLine($"        <td>{itemName}</td>");
                    sb.AppendLine($"        <td class='text-center'>{qty.ToString("N0", Id)}</td>");
                    sb.AppendLine($"        <td class='text-center'>{trip}</td>");

                    for (var idx = 0; idx < totalRoundCount; idx++)
                    {
                        var price = pricesPerRound[idx];
                        if (price.HasValue)
                        {
                            sb.AppendLine(
                                $"        <td class='text-end'>{price.Value.ToString("N0", Id)}</td>"
                            );
                        }
                        else
                        {
                            sb.AppendLine("        <td class='text-center'>No Quote</td>");
                        }
                    }

                    sb.AppendLine(
                        $"        <td class='text-end'>{(total.HasValue ? total.Value.ToString("N0", Id) : "-")}</td>"
                    );
                    sb.AppendLine("      </tr>");
                }

                sb.AppendLine("    </tbody>");
                sb.AppendLine("    <tfoot>");
                sb.AppendLine("      <tr>");

                var colspan = 4 + totalRoundCount; // No + Uraian + Unit + Trip + semua harga
                sb.AppendLine($"        <th colspan='{colspan}'>Tagihan</th>");
                sb.AppendLine($"        <th class='text-end'>{grandTotal.ToString("N0", Id)}</th>");
                sb.AppendLine("      </tr>");
                sb.AppendLine("    </tfoot>");
                sb.AppendLine("  </table>");
                sb.AppendLine("</div>");
            }

            return sb.ToString();
        }

        private static string GeneratePnlEstimateTable(ProfitLoss pnl, Procurement proc)
        {
            var sb = new StringBuilder();

            // --- 1. Ambil Revenue total dari PNL ---

            var items = pnl.Items?.ToList() ?? new List<ProfitLossItem>();
            if (items.Count == 0)
                return "<p class='text-center'>Tidak ada data Profit &amp; Loss.</p>";

            var revenueTotal = items.Sum(i => i.Revenue);
            var pnlItemsByOfferId = items.ToDictionary(i => i.ProcOfferId, i => i);

            var vendorOffers = pnl.VendorOffers?.ToList() ?? new List<VendorOffer>();
            if (vendorOffers.Count == 0)
            {
                sb.AppendLine(
                    "<p class='text-center'>Tidak ada penawaran vendor untuk Profit &amp; Loss summary.</p>"
                );
                return sb.ToString();
            }

            // --- 2. Hitung harga penawaran terendah + profit per vendor ---

            // hasil akhir per vendor (nama, total penawaran, profit, persentase)
            var vendors =
                new List<(Vendor Vendor, decimal Total, decimal Profit, decimal ProfitPercent)>();

            // distinct vendorId, urut nama
            var vendorIds = vendorOffers.Select(vo => vo.VendorId).Distinct().ToList();

            foreach (var vendorId in vendorIds)
            {
                var offersForVendor = vendorOffers.Where(vo => vo.VendorId == vendorId).ToList();

                if (offersForVendor.Count == 0)
                    continue;

                var vendor = offersForVendor.First().Vendor;

                // semua item yang pernah ditawarkan vendor ini
                var itemGroups = offersForVendor.GroupBy(vo => vo.ProcOfferId).ToList();

                // semua round yang pernah dipakai vendor ini
                var rounds = offersForVendor
                    .Select(vo => vo.Round)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();

                decimal? bestTotal = null;
                int? bestRound = null;

                // cari total penawaran TERENDAH per round
                foreach (var round in rounds)
                {
                    decimal roundTotal = 0m;
                    var adaBaris = false;

                    foreach (var itemGroup in itemGroups)
                    {
                        var offerForRound = itemGroup.FirstOrDefault(o => o.Round == round);
                        if (offerForRound == null || offerForRound.Price <= 0)
                            continue;

                        // qty pakai PNL kalau ada, kalau tidak pakai qty dari penawaran vendor
                        pnlItemsByOfferId.TryGetValue(itemGroup.Key, out var pnlItem);
                        var qty = pnlItem?.Quantity ?? offerForRound.Quantity;
                        var trip = offerForRound.Trip;

                        if (qty <= 0)
                            continue;

                        var tripFactor = trip > 0 ? trip : 1; // Trip 0 → abaikan

                        roundTotal += offerForRound.Price * qty * tripFactor;
                        adaBaris = true;
                    }

                    if (!adaBaris)
                        continue;

                    // simpan total terendah;
                    // kalau sama, pilih round paling tinggi (nego terakhir)
                    if (
                        bestTotal == null
                        || roundTotal < bestTotal
                        || (roundTotal == bestTotal && (bestRound == null || round > bestRound))
                    )
                    {
                        bestTotal = roundTotal;
                        bestRound = round;
                    }
                }

                if (bestTotal == null)
                    continue;

                var vendorTotal = bestTotal.Value;

                // profit = revenue - penawaran terendah vendor
                var profit = revenueTotal - vendorTotal;

                // % profit = ((revenue - penawaran)/revenue) * 100
                var profitPercent =
                    revenueTotal > 0
                        ? Math.Round(((revenueTotal - vendorTotal) / revenueTotal) * 100m, 2)
                        : 0m;

                vendors.Add((vendor, vendorTotal, profit, profitPercent));
            }

            // kalau tidak ada vendor yang valid
            if (vendors.Count == 0)
                return "<p class='text-center'>Tidak ada data penawaran yang dapat dihitung.</p>";

            // urut vendor by nama biar rapi
            vendors = vendors.OrderBy(v => v.Vendor.VendorName).ToList();

            // untuk highlight PROFIT & % vendor terbaik (opsional)
            var best = vendors
                .OrderByDescending(v => v.Profit)
                .ThenByDescending(v => v.ProfitPercent)
                .ThenBy(v => v.Total)
                .First();

            var vendorCount = vendors.Count;
            var totalColumns = 2 + vendorCount;

            // --- 3. Bangun HTML tabel (layout mirip contoh) ---

            sb.AppendLine(
                "<table class='table table-bordered table-sm align-middle mb-5 border-black'>"
            );
            sb.AppendLine("  <thead>");
            sb.AppendLine("    <tr>");
            sb.AppendLine(
                $"      <th colspan='{totalColumns}' class='green-header text-center'>Profit &amp; Loss Estimate</th>"
            );
            sb.AppendLine("    </tr>");
            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine("      <th class='green-header' style='width: 1rem'></th>");
            sb.AppendLine("      <th class='green-header' style='width: 20rem'>TAGIHAN PDSI</th>");
            sb.AppendLine(
                $"      <th colspan='{vendorCount}' class='green-header'>PENAWARAN MITRA</th>"
            );
            sb.AppendLine("    </tr>");
            sb.AppendLine("  </thead>");
            sb.AppendLine("  <tbody>");

            // baris: header Revenue + nama vendor
            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine("      <td>&ThinSpace;</td>");
            sb.AppendLine("      <td>Revenue</td>");
            foreach (var v in vendors)
            {
                var name = v.Vendor.VendorName ?? "-";
                sb.AppendLine($"      <td style='width: 17rem'>{name}</td>");
            }
            sb.AppendLine("    </tr>");

            // baris: nilai Revenue & total penawaran vendor (harga terendah)
            sb.AppendLine("    <tr>");
            sb.AppendLine("      <td>Rp</td>");
            sb.AppendLine($"      <td class='text-end'>{revenueTotal.ToString("N0", Id)}</td>");
            foreach (var v in vendors)
            {
                sb.AppendLine($"      <td class='text-end'>{v.Total.ToString("N0", Id)}</td>");
            }
            sb.AppendLine("    </tr>");

            // COST OPERATOR
            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine(
                $"      <td colspan='{totalColumns - 2}' class='fw-semibold'>COST OPERATOR</td>"
            );

            for (var i = 1; i <= vendorCount; i++)
            {
                sb.AppendLine("      <td></td>");
            }

            sb.AppendLine("    </tr>");

            // PROFIT (Rp)
            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine("      <td colspan='2' class='fw-semibold'>PROFIT</td>");
            foreach (var v in vendors)
            {
                var css =
                    v.Vendor.VendorId == best.Vendor.VendorId
                        ? "green-highlight text-end"
                        : "text-end";

                sb.AppendLine($"      <td class='{css}'>Rp {v.Profit.ToString("N0", Id)}</td>");
            }
            sb.AppendLine("    </tr>");

            // % PROFIT
            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine("      <td colspan='2' class='fw-semibold'>%</td>");
            foreach (var v in vendors)
            {
                var css =
                    v.Vendor.VendorId == best.Vendor.VendorId
                        ? "green-highlight text-end"
                        : "text-end";

                sb.AppendLine(
                    $"      <td class='{css}'>{v.ProfitPercent.ToString("N2", Id)}%</td>"
                );
            }
            sb.AppendLine("    </tr>");

            // Adjustment naik 15% (tanpa nilai)
            sb.AppendLine("    <tr>");
            sb.AppendLine("      <td>&ThinSpace;</td>");
            sb.AppendLine("      <td class='text-end'>Adjustment naik 15%</td>");
            foreach (var _ in vendors)
            {
                sb.AppendLine("      <td class='text-end'>&ThinSpace;</td>");
            }
            sb.AppendLine("    </tr>");

            // Potential new Profit (tanpa nilai)
            sb.AppendLine("    <tr>");
            sb.AppendLine("      <td>&ThinSpace;</td>");
            sb.AppendLine("      <td class='text-end'>Potential new Profit</td>");
            foreach (var _ in vendors)
            {
                sb.AppendLine("      <td class='text-end'>&ThinSpace;</td>");
            }
            sb.AppendLine("    </tr>");

            // % Profit Rev (tanpa nilai)
            sb.AppendLine("    <tr class='text-center'>");
            sb.AppendLine("      <td>&ThinSpace;</td>");
            sb.AppendLine("      <td>% Profit Rev</td>");
            foreach (var _ in vendors)
            {
                sb.AppendLine("      <td class='text-end'>&ThinSpace;</td>");
            }
            sb.AppendLine("    </tr>");

            sb.AppendLine("  </tbody>");
            sb.AppendLine("</table>");

            return sb.ToString();
        }

        #endregion
    }
}
