using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private async Task RepopulateVendorChoices(ProfitLossInputViewModel viewModel)
    {
        var vendors = await _vendorQueryService.GetAllVendorsAsync();
        viewModel.VendorChoices = BuildVendorChoices(vendors);

        var procurement = await _queryService.GetProcurementByIdAsync(
            viewModel.ProcurementId
        );

        var submittedItemLookup =
            viewModel
                .Items?.Where(item => !string.IsNullOrEmpty(item.ProcOfferId))
                .ToDictionary(
                    item => item.ProcOfferId,
                    item => (Unit: item.UnitItems, UnitRevenue: item.UnitRevenue),
                    StringComparer.OrdinalIgnoreCase
                ) ?? new Dictionary<string, (string? Unit, string? UnitRevenue)>();

        viewModel.OfferItems = (procurement?.ProcOffers ?? [])
            .Select(offer =>
            {
                submittedItemLookup.TryGetValue(offer.ProcOfferId, out var submittedItem);
                return new ProcOfferLiteVm
                {
                    ProcOfferId = offer.ProcOfferId,
                    ItemPenawaran = offer.ItemPenawaran,
                    Quantity = offer.Qty,
                    Unit = submittedItem.Unit ?? offer.Unit,
                    UnitRevenue = submittedItem.UnitRevenue ?? offer.UnitRevenue,
                };
            })
            .ToList();

        if (procurement != null)
            await SetProfitLossViewBagsAsync(procurement);

        BackfillProfitLossItemQuantity(viewModel);
    }

    private static void BackfillProfitLossItemQuantity(ProfitLossInputViewModel viewModel)
    {
        if (viewModel.Items == null || viewModel.Items.Count == 0)
            return;

        var qtyMap = viewModel.OfferItems.ToDictionary(
            offer => offer.ProcOfferId,
            offer => offer.Quantity,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var item in viewModel.Items)
        {
            if (item.Quantity <= 0 && qtyMap.TryGetValue(item.ProcOfferId, out var quantity))
                item.Quantity = (int)quantity;
        }
    }
}
