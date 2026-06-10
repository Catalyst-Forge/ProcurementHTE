using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Services;

namespace ProcurementHTE.Tests.Services;

public class ProfitLossCalculatorTests
{
    [Fact]
    public void PickBestVendor_PrefersCompleteVendorOverCheaperPartialVendor()
    {
        var offers = new List<VendorOffer>
        {
            CreateOffer("vendor-a", "item-a", price: 100m),
            CreateOffer("vendor-a", "item-b", price: 200m),
            CreateOffer("vendor-b", "item-a", price: 10m),
        };

        var result = ProfitLossCalculator.PickBestVendor(offers, ["item-a", "item-b"]);

        Assert.Equal("vendor-a", result.vendorId);
        Assert.Equal(300m, result.totalFinal);
    }

    [Fact]
    public void PickBestVendor_FallsBackToLowestPartialVendorWhenNoneAreComplete()
    {
        var offers = new List<VendorOffer>
        {
            CreateOffer("vendor-a", "item-a", price: 100m),
            CreateOffer("vendor-b", "item-b", price: 20m),
        };

        var result = ProfitLossCalculator.PickBestVendor(offers, ["item-a", "item-b"]);

        Assert.Equal("vendor-b", result.vendorId);
        Assert.Equal(20m, result.totalFinal);
    }

    [Fact]
    public void PickBestVendor_UsesMinimumRoundPriceAndLastRoundQuantities()
    {
        var offers = new List<VendorOffer>
        {
            CreateOffer("vendor-a", "item-a", round: 1, price: 100m, quantity: 2, trip: 2m),
            CreateOffer("vendor-a", "item-a", round: 2, price: 80m, quantity: 3, trip: 4m),
        };

        var result = ProfitLossCalculator.PickBestVendor(offers, ["item-a"]);

        Assert.Equal("vendor-a", result.vendorId);
        Assert.Equal(960m, result.totalFinal);
        Assert.Equal(960m, result.finalPerItem["item-a"]);
    }

    [Fact]
    public void ComputeVendorItemCost_DefaultsNonPositiveQuantityAndTripToOne()
    {
        var result = ProfitLossCalculator.ComputeVendorItemCost(
            price: 50m,
            quantity: 0m,
            trip: 0m
        );

        Assert.Equal(50m, result);
    }

    [Fact]
    public void SafeSum_ReturnsDecimalMaxValueWhenAdditionWouldOverflow()
    {
        var result = ProfitLossCalculator.SafeSum([decimal.MaxValue, 1m]);

        Assert.Equal(decimal.MaxValue, result);
    }

    private static VendorOffer CreateOffer(
        string vendorId,
        string procOfferId,
        int round = 1,
        decimal price = 1m,
        int quantity = 1,
        decimal trip = 1m
    )
    {
        return new VendorOffer
        {
            VendorId = vendorId,
            ProcOfferId = procOfferId,
            Round = round,
            Price = price,
            QuantityItem = quantity,
            QuantityOfUnit = trip,
            ProcurementId = "procurement-1",
            ProfitLossId = "profit-loss-1",
            UnitTypeId = "unit-type-1",
        };
    }
}
