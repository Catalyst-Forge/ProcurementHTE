using ProcurementHTE.Core.Finance;

namespace ProcurementHTE.Core.Models.DTOs;

/// <summary>The list an Excel file is imported into.</summary>
public enum FinanceImportTarget
{
    ApPo,
    ApInvoice,
    Accrual,
    Ldp,
}

/// <summary>One cell an import will change: the job, the column, and its value before and after.</summary>
/// <param name="Value">What goes to the cell's save, as the table's own input sends it; null clears it.</param>
public sealed record FinanceImportChange(
    string ProcurementId,
    string Job,
    FinanceField Field,
    string Label,
    string From,
    string To,
    string? Value
);

/// <summary>A row the import leaves alone, with the reason. Row is the row number in the sheet.</summary>
public sealed record FinanceImportRejection(int Row, string Job, string Reason);

/// <summary>What an import would do, worked out before anything is saved.</summary>
public sealed record FinanceImportPlan(
    FinanceImportTarget Target,
    IReadOnlyList<FinanceImportChange> Changes,
    int Unchanged,
    IReadOnlyList<FinanceImportRejection> Rejected
)
{
    public int Jobs => Changes.Select(change => change.ProcurementId).Distinct().Count();
}

/// <summary>What an import saved. A change can still fail when the job changed after the preview.</summary>
public sealed record FinanceImportResult(int Saved, IReadOnlyList<(FinanceImportChange Change, string Reason)> Failed);

/// <summary>A file the import cannot use. The message is shown to the user.</summary>
public sealed class FinanceImportException(string message) : Exception(message);
