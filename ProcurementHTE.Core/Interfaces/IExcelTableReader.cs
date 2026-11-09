using ProcurementHTE.Core.Finance;

namespace ProcurementHTE.Core.Interfaces;

/// <summary>Reads the first sheet of an .xlsx as a header row and data rows.</summary>
public interface IExcelTableReader
{
    /// <exception cref="Models.DTOs.FinanceImportException">The file is not a workbook this can read.</exception>
    ExcelSheet Read(Stream workbook);
}
