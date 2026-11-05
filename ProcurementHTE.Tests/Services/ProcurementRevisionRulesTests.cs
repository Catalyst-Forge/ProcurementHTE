using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Services;

namespace ProcurementHTE.Tests.Services;

public class ProcurementRevisionRulesTests
{
    [Theory]
    [InlineData(300_000_000, false)]
    [InlineData(300_000_001, true)]
    public void VicePresidentApproval_StartsAbove300Million(decimal contractTotal, bool required)
    {
        Assert.Equal(required, ApprovalConstants.RequiresVpApproval(contractTotal));
    }

    [Theory]
    [InlineData(ContractType.Spot, true)]
    [InlineData(ContractType.STB, true)]
    [InlineData(ContractType.LTC, false)]
    [InlineData(ContractType.RO, false)]
    public void RaNumber_IsOnlyForContractsWithRiskAssessment(ContractType contract, bool required)
    {
        var procurement = new Procurement { ContractType = contract };

        Assert.Equal(required, procurement.RequiresRaNumber);
    }

    [Fact]
    public void UnitPrice_ComesFromTheTypedRevenue()
    {
        var item = new ProfitLossItem { ItemName = "Tronton", UnitQty = 5, Revenue = 600_000_000m };

        Assert.Equal(120_000_000m, ProfitLossCalculator.UnitPrice(item));
    }

    [Fact]
    public void UnitPrice_IsZeroWithoutUnits()
    {
        var item = new ProfitLossItem { ItemName = "Tronton", UnitQty = 0, Revenue = 600_000_000m };

        Assert.Equal(0m, ProfitLossCalculator.UnitPrice(item));
    }

    [Fact]
    public void ProfitLoss_ForAngkutan_NoLongerNeedsADistance()
    {
        var calculation = new JobTypeCalculationService();

        var error = Record.Exception(() =>
            calculation.ValidateRequiredFields(new ProfitLoss { ProcurementId = "proc" }, "Angkutan")
        );

        Assert.Null(error);
    }
}

public class ContractAndPrRulesTests
{
    private const string Spmp = "Surat Perintah Mulai Pekerjaan (SPMP)";
    private const string WorkOrder = "Permintaan Pekerjaan";

    [Theory]
    [InlineData(ContractType.LTC, Spmp, true)]
    [InlineData(ContractType.LTC, "Memorandum", false)]
    [InlineData(ContractType.LTC, "Profit & Loss", false)]
    [InlineData(ContractType.RO, "Memorandum", true)]
    [InlineData(ContractType.RO, Spmp, false)]
    [InlineData(ContractType.RO, ContractDocumentRules.FinancialStatement, true)]
    [InlineData(ContractType.Spot, ContractDocumentRules.FinancialStatement, false)]
    [InlineData(ContractType.Spot, "Profit & Loss", true)]
    [InlineData(ContractType.STB, "Risk Assessment (RA)", true)]
    public void DocumentSet_FollowsTheContract(ContractType contract, string document, bool applies)
    {
        Assert.Equal(applies, ContractDocumentRules.Applies(contract, document));
    }

    [Fact]
    public void WorkOrder_IsOptionalOnlyForRo()
    {
        Assert.True(ContractDocumentRules.IsOptional(ContractType.RO, WorkOrder));
        Assert.False(ContractDocumentRules.IsOptional(ContractType.LTC, WorkOrder));
    }

    [Fact]
    public void PrStep_OpensOnlyWhenEveryLiveJobReachedIt()
    {
        Assert.Null(
            PrSubmitSteps.Current([ProcurementStatus.OnSubmitISPA, ProcurementStatus.WaitingApprovalManager])
        );
        Assert.Equal(
            ProcurementStatus.OnSubmitISPA,
            PrSubmitSteps.Current([ProcurementStatus.OnSubmitISPA, ProcurementStatus.OnSubmitISPA])
        );
    }

    [Fact]
    public void PrStep_IgnoresCancelledJobs()
    {
        Assert.Equal(
            ProcurementStatus.OnSubmitPO,
            PrSubmitSteps.Current([ProcurementStatus.OnSubmitPO, ProcurementStatus.Cancelled])
        );
        Assert.Null(PrSubmitSteps.Current([ProcurementStatus.Cancelled]));
    }

    [Fact]
    public void PrStatus_NoLongerWaitsOnCancelledJobs()
    {
        var pr = new PurchaseRequisition
        {
            Procurements =
            [
                new Procurement { ProcurementStatus = ProcurementStatus.DonePO },
                new Procurement { ProcurementStatus = ProcurementStatus.Cancelled },
            ],
        };

        Assert.Equal(PurchaseRequisitionStatus.DonePO, pr.DerivedStatus);
    }

    [Theory]
    [InlineData(300_000_000, ApprovalConstants.APPROVAL_LEVEL_MANAGER)]
    [InlineData(300_000_001, ApprovalConstants.APPROVAL_LEVEL_VP)]
    [InlineData(20_000_000_000, ApprovalConstants.APPROVAL_LEVEL_VP)]
    public void Approval_StopsAtVicePresident(decimal contractTotal, int level)
    {
        Assert.Equal(level, ApprovalConstants.GetRequiredApprovalLevel(contractTotal));
    }

    [Theory]
    [InlineData(ContractType.LTC, true)]
    [InlineData(ContractType.RO, true)]
    [InlineData(ContractType.Spot, false)]
    public void SimpleProfitLoss_IsForLtcAndRo(ContractType contract, bool simple)
    {
        Assert.Equal(simple, new Procurement { ContractType = contract }.UsesSimpleProfitLoss);
    }
}

public class RaNumberChangeTests
{
    [Theory]
    [InlineData(ContractType.Spot, ProcurementStatus.OnSubmitPO, true)]
    [InlineData(ContractType.Spot, ProcurementStatus.DonePO, false)]
    [InlineData(ContractType.STB, ProcurementStatus.Cancelled, false)]
    [InlineData(ContractType.LTC, ProcurementStatus.OnSubmitPO, false)]
    public void RaNumber_StopsChangingOnceTheJobEnds(ContractType contract, ProcurementStatus status, bool allowed)
    {
        var procurement = new Procurement { ContractType = contract, ProcurementStatus = status };

        Assert.Equal(allowed, procurement.CanChangeRaNumber);
    }
}
