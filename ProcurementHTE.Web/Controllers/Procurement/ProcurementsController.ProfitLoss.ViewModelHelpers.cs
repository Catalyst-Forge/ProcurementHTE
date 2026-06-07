using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private static ProfitLossInputViewModel BuildProfitLossInputViewModel(
        Procurement procurement,
        IEnumerable<Vendor> vendors
    )
    {
        var offerItems = BuildProfitLossOfferItems(procurement);
        return new ProfitLossInputViewModel
        {
            ProcurementId = procurement.ProcurementId,
            JobTypeName = procurement.JobType?.TypeName,
            VendorChoices = BuildVendorChoices(vendors),
            OfferItems = offerItems,
            Items = BuildCreateProfitLossItems(offerItems),
        };
    }

    private static List<ProcOfferLiteVm> BuildProfitLossOfferItems(Procurement procurement)
    {
        return (procurement.ProcOffers ?? [])
            .Select(offer => new ProcOfferLiteVm
            {
                ProcOfferId = offer.ProcOfferId,
                ItemPenawaran = offer.ItemPenawaran,
                Quantity = offer.Qty,
                Unit = offer.Unit,
                UnitRevenue = offer.UnitRevenue,
            })
            .ToList();
    }

    private static List<ItemTariffInputVm> BuildCreateProfitLossItems(
        IEnumerable<ProcOfferLiteVm> offerItems
    )
    {
        return offerItems
            .Select(offer => new ItemTariffInputVm
            {
                ProcOfferId = offer.ProcOfferId,
                Quantity = (int)Math.Round(offer.Quantity),
                TarifAwal = null,
                TarifAdd = null,
                KmPer25 = null,
                OperatorCost = null,
            })
            .ToList();
    }

    private static List<VendorChoiceViewModel> BuildVendorChoices(IEnumerable<Vendor> vendors)
    {
        return vendors
            .Select(vendor => new VendorChoiceViewModel
            {
                Id = vendor.VendorId,
                Name = vendor.VendorName,
            })
            .ToList();
    }

    private async Task SetProfitLossViewBagsAsync(Procurement procurement)
    {
        ViewBag.ProcNum = procurement.ProcNum;
        ViewBag.IssueDate = procurement.CreatedAt.ToString("d MMMM yyyy");
        ViewBag.JobTypeName = procurement.JobType?.TypeName ?? "Unknown";
        ViewBag.UnitTypes = await _unitTypeRepository.GetActiveAsync();
    }
}
