using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Interfaces;

/// <summary>Imports an edited export of AP-PO, AP-Invoice, Data Accrual, or the LDP back into the jobs.</summary>
public interface IFinanceImportService
{
    /// <summary>What the file would change. Nothing is saved.</summary>
    /// <exception cref="FinanceImportException">The file is not an export of this list.</exception>
    Task<FinanceImportPlan> PreviewAsync(FinanceImportTarget target, ExcelSheet sheet, CancellationToken ct = default);

    /// <summary>Saves each change the way filling its cell saves it.</summary>
    Task<FinanceImportResult> ApplyAsync(FinanceImportPlan plan, string userId, CancellationToken ct = default);
}
