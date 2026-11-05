using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Constants;

/// <summary>
/// LTC and RO run on a short document set; every other contract keeps the
/// full set of its job type.
/// </summary>
public static class ContractDocumentRules
{
    public const string FinancialStatement = "Financial Statement";
    private const string WorkOrder = "Permintaan Pekerjaan";
    private const string Spmp = "Surat Perintah Mulai Pekerjaan (SPMP)";
    private const string Memorandum = "Memorandum";

    public static bool Applies(ContractType contract, string? documentName) =>
        contract switch
        {
            ContractType.LTC => documentName is Spmp or WorkOrder or FinancialStatement,
            ContractType.RO => documentName is Memorandum or WorkOrder or FinancialStatement,
            _ => documentName != FinancialStatement,
        };

    public static bool IsOptional(ContractType contract, string? documentName) =>
        contract == ContractType.RO && documentName == WorkOrder;
}
