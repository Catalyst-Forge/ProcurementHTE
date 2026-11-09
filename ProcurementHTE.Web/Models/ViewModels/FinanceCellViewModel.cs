using ProcurementHTE.Core.Finance;

namespace ProcurementHTE.Web.Models.ViewModels;

/// <summary>One clickable cell of a finance table (Views/Shared/_FinanceCell.cshtml).</summary>
public sealed record FinanceCellViewModel(
    string SaveUrl,
    FinanceField Field,
    string? Text,
    DateTime? Date,
    bool CanEdit,
    decimal? Money = null
)
{
    public FinanceFieldSpec Spec => FinanceTable.Fields[Field];

    public string InputKind =>
        Spec.Input switch
        {
            FinanceFieldInput.Date => "date",
            FinanceFieldInput.Money => "money",
            _ => "text",
        };

    public string Display =>
        Spec.Input switch
        {
            FinanceFieldInput.Date => FinanceTable.FormatDate(Date),
            FinanceFieldInput.Money => FinanceTable.FormatMoney(Money),
            _ => string.IsNullOrWhiteSpace(Text) ? "-" : Text!,
        };

    public bool IsEmpty =>
        Spec.Input switch
        {
            FinanceFieldInput.Date => !Date.HasValue,
            FinanceFieldInput.Money => !Money.HasValue,
            _ => string.IsNullOrWhiteSpace(Text),
        };

    /// <summary>What the edit dialog starts with: yyyy-MM-dd for a date, 1.500.000 for money.</summary>
    public string InputValue =>
        Spec.Input switch
        {
            FinanceFieldInput.Date => Date?.ToString("yyyy-MM-dd") ?? string.Empty,
            FinanceFieldInput.Money => Money.HasValue ? FinanceTable.FormatMoney(Money) : string.Empty,
            _ => Text ?? string.Empty,
        };
}
