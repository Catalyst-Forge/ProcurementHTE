namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static void ApplyProcurementTableTokens(HtmlTokenReplacementContext context)
        {
            var proc = context.Procurement;
            if (proc.ProcDetails != null && proc.ProcDetails.Count != 0)
            {
                context.Replace("ProcDetailsTable", GenerateDetailsTable(proc.ProcDetails));
            }
            else
            {
                context.Replace(
                    "ProcDetailsTable",
                    "<tr><td colspan='4' class='text-center'>Tidak ada detail</td></tr>"
                );
            }

            if (proc.ProcOffers != null && proc.ProcOffers.Count != 0)
            {
                var firstOffer = proc.ProcOffers.First();
                context.Replace("Items.ItemPenawaran", firstOffer.ItemPenawaran);
                context.Replace("Items.Quantity", firstOffer.Qty.ToString("N0", Id));
                context.Replace("Items.Unit", firstOffer.Unit);
                context.Replace("ProcOffersTable", GenerateOffersTable(proc.ProcOffers));
                context.Replace("ProcOffersList", GenerateOffersList(proc.ProcOffers));
            }
            else
            {
                context.Replace("Items.ItemPenawaran", "-");
                context.Replace("Items.Quantity", "-");
                context.Replace("Items.Unit", "-");
                context.Replace(
                    "ProcOffersTable",
                    "<tr><td colspan='4' class='text-center'>Tidak ada item penawaran</td></tr>"
                );
            }
        }
    }
}
