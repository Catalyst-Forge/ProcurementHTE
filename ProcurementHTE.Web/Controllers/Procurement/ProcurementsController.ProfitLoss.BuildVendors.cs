using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private static List<VendorItemOfferInputVm> BuildProfitLossVendorModels(
        IEnumerable<VendorItemOffersDto> dtoVendors,
        Procurement procurement
    )
    {
        var allProcOfferIds = (procurement.ProcOffers ?? [])
            .Select(offer => offer.ProcOfferId)
            .ToList();

        return dtoVendors
            .Where(vendor => !string.IsNullOrWhiteSpace(vendor.VendorId))
            .GroupBy(vendor => vendor.VendorId, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildProfitLossVendorModel(group.ToList(), allProcOfferIds))
            .ToList();
    }

    private static VendorItemOfferInputVm BuildProfitLossVendorModel(
        List<VendorItemOffersDto> vendorEntries,
        List<string> procOfferIds
    )
    {
        var firstEntry = vendorEntries.First();
        return new VendorItemOfferInputVm
        {
            VendorId = firstEntry.VendorId,
            Letters = firstEntry.Letters?.ToList() ?? [],
            LetterDocIds = (firstEntry.LetterDocIds ?? []).Select(id => id ?? string.Empty).ToList(),
            Items = BuildVendorItemsForProcOffers(vendorEntries, procOfferIds),
        };
    }

    private static List<VendorOfferPerItemInputVm> BuildVendorItemsForProcOffers(
        IEnumerable<VendorItemOffersDto> vendorEntries,
        List<string> procOfferIds
    )
    {
        var existingVendorItems = vendorEntries
            .SelectMany(vendor => vendor.Items ?? [])
            .GroupBy(item => item.ProcOfferId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        return procOfferIds
            .Select(procOfferId =>
                existingVendorItems.TryGetValue(procOfferId, out var existing)
                    ? BuildExistingVendorItem(existing)
                    : BuildNewVendorItem(procOfferId)
            )
            .ToList();
    }

    private static VendorOfferPerItemInputVm BuildExistingVendorItem(VendorOfferPerItemDto existing)
    {
        return new VendorOfferPerItemInputVm
        {
            ProcOfferId = existing.ProcOfferId,
            Prices = existing.Prices?.ToList() ?? [],
            Quantity = existing.Quantity,
            Trip = existing.Trip,
            IsIncluded = existing.IsIncluded,
        };
    }

    private static VendorOfferPerItemInputVm BuildNewVendorItem(string procOfferId)
    {
        return new VendorOfferPerItemInputVm
        {
            ProcOfferId = procOfferId,
            Prices = [],
            Quantity = 0,
            Trip = 0,
            IsIncluded = true,
        };
    }
}
