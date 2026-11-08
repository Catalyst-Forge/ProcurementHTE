namespace ProcurementHTE.Core.Interfaces;

public interface IExcelExporter
{
    /// <summary>One sheet with a header row. Dates and numbers keep their type in Excel.</summary>
    byte[] Export(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows);
}
