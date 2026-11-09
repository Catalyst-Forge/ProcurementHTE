using ClosedXML.Excel;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Services;

/// <summary>
/// Reads an uploaded export back: the first sheet, row 1 as headers, and each later row as header
/// to value. A date cell gives a DateTime, a number cell a decimal, text is trimmed, and an empty
/// cell is null. Rows with nothing in them are skipped.
/// </summary>
public sealed class ExcelTableReader : IExcelTableReader
{
    public ExcelSheet Read(Stream workbook)
    {
        XLWorkbook book;
        try
        {
            book = new XLWorkbook(workbook);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new FinanceImportException("File tidak bisa dibaca. Pakai file Excel (.xlsx) hasil Ekspor Excel.");
        }

        using (book)
        {
            var sheet = book.Worksheets.FirstOrDefault();
            var used = sheet?.RangeUsed();
            if (sheet is null || used is null)
                return new ExcelSheet([], []);

            var headerRow = used.FirstRow().RowNumber();
            var columns = new List<(int Column, string Header)>();
            for (var column = used.FirstColumn().ColumnNumber(); column <= used.LastColumn().ColumnNumber(); column++)
            {
                var header = sheet.Cell(headerRow, column).GetString().Trim();
                if (header.Length > 0 && columns.All(existing => !string.Equals(existing.Header, header, StringComparison.OrdinalIgnoreCase)))
                    columns.Add((column, header));
            }

            var rows = new List<ExcelSheetRow>();
            for (var number = headerRow + 1; number <= used.LastRow().RowNumber(); number++)
            {
                var cells = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (column, header) in columns)
                    cells[header] = Value(sheet.Cell(number, column));

                if (cells.Values.Any(value => value is not null))
                    rows.Add(new ExcelSheetRow(number, cells));
            }

            return new ExcelSheet(columns.Select(column => column.Header).ToList(), rows);
        }
    }

    private static object? Value(IXLCell cell)
    {
        var value = cell.Value;
        if (value.IsBlank)
            return null;
        if (value.IsDateTime)
            return value.GetDateTime();
        if (value.IsNumber)
            return (decimal)value.GetNumber();
        if (value.IsBoolean)
            return value.GetBoolean() ? "Ya" : "Tidak";

        var text = cell.GetString().Trim();
        return text.Length == 0 ? null : text;
    }
}
