using System.Text;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static string GenerateItemsTable(
            ICollection<ProfitLossItem> items,
            string? docType,
            string? jobTypeName = null
        )
        {
            var sb = new StringBuilder();
            var no = 1;

            foreach (var item in items)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"  <td class='text-center'>{no++}</td>");
                sb.AppendLine($"  <td>{item.ProcOffer?.ItemPenawaran ?? "-"}</td>");
                sb.AppendLine($"  <td class='text-center'>{item.UnitQty}</td>");
                sb.AppendLine($"  <td class='text-center'>{item.ProcOffer?.Unit ?? "-"}</td>");
                AppendItemFinancialCells(sb, item, docType, jobTypeName);
                sb.AppendLine("</tr>");
            }

            return sb.ToString();
        }

        private static void AppendItemFinancialCells(
            StringBuilder sb,
            ProfitLossItem item,
            string? docType,
            string? jobTypeName
        )
        {
            if (docType == "OwnerEstimate")
            {
                var price = item.BasePrice + ((item.TarifAdd ?? 0) * (item.KmPer25 ?? 0));
                sb.AppendLine($"  <td class='text-end'>{price.ToString("C0", Id)}</td>");
                sb.AppendLine($"  <td class='text-end'>{(price * item.UnitQty).ToString("C0", Id)}</td>");
                return;
            }

            if (docType != "ProfitLoss")
                return;

            if (jobTypeName == "StandBy" || jobTypeName == "Sewa Unit" || jobTypeName == "Moving")
            {
                var quantity = item.Quantity ?? 1;
                var unitRevenue = item.ProcOffer?.UnitRevenue ?? "-";
                sb.AppendLine($"  <td class='text-center'>{quantity.ToString("0.##", Id)}</td>");
                sb.AppendLine($"  <td class='text-center'>{unitRevenue}</td>");
                sb.AppendLine($"  <td class='text-end'>{item.BasePrice.ToString("N0", Id)}</td>");
                sb.AppendLine($"  <td class='text-end'>{item.Revenue.ToString("C0", Id)}</td>");
                return;
            }

            sb.AppendLine($"  <td class='text-end'>{item.BasePrice.ToString("N0", Id)}</td>");
            sb.AppendLine($"  <td class='text-end'>{(item.TarifAdd ?? 0).ToString("N0", Id)}</td>");
            sb.AppendLine($"  <td class='text-center'>{item.KmPer25 ?? 0}</td>");
            sb.AppendLine($"  <td class='text-end'>{(item.OperatorCost ?? 0).ToString("N0", Id)}</td>");
            var revenue = (item.BasePrice + ((item.TarifAdd ?? 0) * (item.KmPer25 ?? 0))) * item.UnitQty;
            sb.AppendLine($"  <td class='text-end'>{revenue.ToString("C0", Id)}</td>");
        }

        private static string GenerateItemsTableHeader(string? jobTypeName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<tr class='text-center'>");
            sb.AppendLine("  <th scope='col' style='width: 1.5rem'>No</th>");
            sb.AppendLine("  <th class='text-start' scope='col' style='width: 14rem'>Item</th>");
            sb.AppendLine("  <th scope='col' style='width: 3rem'>Qty Items</th>");
            sb.AppendLine("  <th scope='col' style='width: 4rem'>Unit Items</th>");

            if (jobTypeName == "StandBy" || jobTypeName == "Sewa Unit")
            {
                AppendQuantityRevenueHeader(sb, "Durasi");
            }
            else if (jobTypeName == "Moving")
            {
                AppendQuantityRevenueHeader(sb, "Qty Revenue");
            }
            else
            {
                AppendAngkutanHeader(sb);
            }

            sb.AppendLine("</tr>");
            return sb.ToString();
        }

        private static void AppendAngkutanHeader(StringBuilder sb)
        {
            sb.AppendLine("  <th scope='col' style='width: 7rem'>Tarif 400 km</th>");
            sb.AppendLine("  <th scope='col' style='width: 6rem'>Tarif Add</th>");
            sb.AppendLine("  <th scope='col' style='width: 4rem'>KM / 25 Km</th>");
            sb.AppendLine("  <th scope='col' style='width: 7rem'>Operator Cost</th>");
            sb.AppendLine("  <th scope='col' style='width: 7rem'>Jumlah</th>");
        }

        private static void AppendQuantityRevenueHeader(StringBuilder sb, string quantityLabel)
        {
            sb.AppendLine($"  <th scope='col' style='width: 4rem'>{quantityLabel}</th>");
            sb.AppendLine("  <th scope='col' style='width: 5rem'>Unit Revenue</th>");
            sb.AppendLine("  <th scope='col' style='width: 7rem'>Base Price</th>");
            sb.AppendLine("  <th scope='col' style='width: 7rem'>Jumlah</th>");
        }

        private static int GetItemsTableColspan(string? jobTypeName)
        {
            return jobTypeName == "StandBy" || jobTypeName == "Sewa Unit" || jobTypeName == "Moving"
                ? 7
                : 8;
        }
    }
}
