using ClosedXML.Excel;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Infrastructure.Services;

public sealed class ExcelExporter : IExcelExporter
{
    private const int MaxColumnWidth = 60;
    private const int RowsMeasured = 200;

    public byte[] Export(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);
        var widths = headers.Select(header => header.Length).ToArray();

        for (var column = 0; column < headers.Count; column++)
            sheet.Cell(1, column + 1).Value = headers[column];
        sheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 1;
        foreach (var row in rows)
        {
            rowNumber++;
            for (var column = 0; column < headers.Count && column < row.Count; column++)
            {
                var cell = sheet.Cell(rowNumber, column + 1);
                var text = Write(cell, row[column]);
                if (rowNumber <= RowsMeasured)
                    widths[column] = Math.Max(widths[column], text.Length);
            }
        }

        // Widths are measured from text, not fonts: the server has no fonts to measure with.
        for (var column = 0; column < headers.Count; column++)
            sheet.Column(column + 1).Width = Math.Min(widths[column] + 2, MaxColumnWidth);

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(rowNumber, 1), Math.Max(headers.Count, 1)).SetAutoFilter();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string Write(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case DateTime date:
                cell.Value = date;
                cell.Style.DateFormat.Format = date.TimeOfDay == TimeSpan.Zero ? "dd/mm/yyyy" : "dd/mm/yyyy hh:mm";
                return "00/00/0000";
            case decimal number:
                cell.Value = number;
                cell.Style.NumberFormat.Format = number % 1 == 0 ? "#,##0" : "#,##0.00";
                return number.ToString("N0");
            case int or long or double:
                cell.Value = Convert.ToDouble(value);
                return value.ToString() ?? string.Empty;
            case bool flag:
                cell.Value = flag ? "Ya" : "Tidak";
                return "Tidak";
            default:
                var text = value.ToString() ?? string.Empty;
                cell.Value = text;
                return text;
        }
    }
}
