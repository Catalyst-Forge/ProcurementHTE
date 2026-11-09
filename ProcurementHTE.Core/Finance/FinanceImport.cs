using System.Globalization;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Finance;

/// <summary>A job as an import compares it: its label, whether it is cancelled, and each fill-in value now.</summary>
public sealed record FinanceImportJob(string ProcurementId, string Label, bool IsCancelled, IReadOnlyDictionary<FinanceField, string?> Values);

/// <summary>One sheet of an uploaded workbook: its header row, and each row as header to value.</summary>
public sealed record ExcelSheet(IReadOnlyList<string> Headers, IReadOnlyList<ExcelSheetRow> Rows);

/// <summary>A row of a sheet; Number is the row number Excel shows.</summary>
public sealed record ExcelSheetRow(int Number, IReadOnlyDictionary<string, object?> Cells);

/// <summary>
/// Excel round trips for the finance tables and the LDP: the export's columns, and the plan an
/// uploaded export makes. A row is matched by its ID Docutrax column; each fill-in column the file
/// has is read the way the table's own input reads it and compared with the value now. A column
/// the file lacks is left alone; an empty cell clears the value. Saving goes through the same
/// path as filling the cell (IProcurementTrackingService.UpdateFinanceFieldAsync).
/// </summary>
public static class FinanceImport
{
    public const string IdHeader = "ID Docutrax";

    private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "d MMM yyyy", "dd MMM yyyy", "d MMMM yyyy",
    ];

    /// <summary>The LDP export's fill-in columns, by the headers it writes (LdpController.Export).</summary>
    public static readonly IReadOnlyDictionary<string, FinanceField> LdpColumns =
        new Dictionary<string, FinanceField>(StringComparer.OrdinalIgnoreCase)
        {
            ["No PR"] = FinanceField.PrNumber,
            ["Nilai Accrual"] = FinanceField.NilaiAccrual,
            ["Nilai Realisasi"] = FinanceField.NilaiRealisasi,
            ["No PO"] = FinanceField.PoNumber,
            ["Tgl Submit INPRO"] = FinanceField.InproDate,
            ["Submit Hard Copy"] = FinanceField.HardcopyDate,
            ["PIC AP-PO"] = FinanceField.PicAppo,
            ["No Invoice"] = FinanceField.InvoiceNumber,
            ["Tgl Invoice"] = FinanceField.InvoiceDate,
            ["SA No"] = FinanceField.SaNo,
            ["Tgl SA"] = FinanceField.SaDate,
            ["SP3 No"] = FinanceField.Sp3No,
            ["Tgl SP3"] = FinanceField.Sp3Date,
            ["Submit Loket"] = FinanceField.LoketDate,
            ["PIC AP-Invoice"] = FinanceField.PicApInvoice,
            ["No Accrual"] = FinanceField.NoAccrual,
        };

    public static FinanceTableKind KindOf(FinanceImportTarget target) =>
        target switch
        {
            FinanceImportTarget.ApPo => FinanceTableKind.ApPo,
            FinanceImportTarget.ApInvoice => FinanceTableKind.ApInvoice,
            FinanceImportTarget.Accrual => FinanceTableKind.Accrual,
            _ => throw new ArgumentOutOfRangeException(nameof(target), "The LDP is not a finance table."),
        };

    /// <summary>The fill-in columns an import reads, by header.</summary>
    public static IReadOnlyDictionary<string, FinanceField> Columns(FinanceImportTarget target) =>
        target == FinanceImportTarget.Ldp
            ? LdpColumns
            : FinanceTable.Fields.Values
                .Where(spec => spec.Table == KindOf(target))
                .ToDictionary(spec => spec.Label, spec => spec.Field, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A finance table's export: ID Docutrax first, what the table shows, the fill-in columns named
    /// as the import reads them, then the status. Dates and amounts keep their type.
    /// </summary>
    public static IReadOnlyList<(string Header, Func<FinanceTableRow, object?> Value)> ExportColumns(FinanceTableKind kind)
    {
        var columns = new List<(string Header, Func<FinanceTableRow, object?> Value)>
        {
            (IdHeader, row => row.ProcurementId),
            ("SPMP", row => row.Spmp),
            ("Job Name", row => row.JobName),
            ("Project Region", row => row.ProjectRegion),
            ("Start Date", row => row.StartDate),
            ("Vendor", row => row.Vendor),
        };
        if (kind is FinanceTableKind.ApPo or FinanceTableKind.ApInvoice or FinanceTableKind.Accrual)
            columns.Add(("Nilai P&L", row => row.NilaiPnl));

        foreach (var spec in FinanceTable.Fields.Values.Where(spec => spec.Table == kind))
            columns.Add((spec.Label, row => Typed(row, spec.Field)));

        columns.Add(("Status", row => Status(row, kind)));
        return columns;
    }

    private static string Status(FinanceTableRow row, FinanceTableKind kind) =>
        row.IsCancelled ? "Cancel"
        : (kind switch
        {
            FinanceTableKind.ApPo => row.ApPoDone,
            FinanceTableKind.ApInvoice => row.ApInvoiceDone,
            FinanceTableKind.Accrual => row.AccrualDone,
            _ => row.ArDone,
        }) ? "Done"
        : "On Progress";

    public static object? Typed(FinanceTableRow row, FinanceField field) =>
        field switch
        {
            FinanceField.PrNumber => row.PrNumber,
            FinanceField.NilaiAccrual => row.NilaiAccrual,
            FinanceField.NilaiRealisasi => row.NilaiRealisasi,
            FinanceField.PoNumber => row.PoNumber,
            FinanceField.InproDate => row.InproDate,
            FinanceField.HardcopyDate => row.HardcopyDate,
            FinanceField.PicAppo => row.PicAppo,
            FinanceField.InvoiceNumber => row.InvoiceNumber,
            FinanceField.InvoiceDate => row.InvoiceDate,
            FinanceField.SaNo => row.SaNo,
            FinanceField.SaDate => row.SaDate,
            FinanceField.Sp3No => row.Sp3No,
            FinanceField.Sp3Date => row.Sp3Date,
            FinanceField.LoketDate => row.LoketDate,
            FinanceField.PicApInvoice => row.PicApInvoice,
            FinanceField.PicAr => row.PicAr,
            FinanceField.NoAccrual => row.NoAccrual,
            _ => null,
        };

    /// <summary>A finance table row as an import compares it.</summary>
    public static FinanceImportJob FromRow(FinanceTableRow row) =>
        new(
            row.ProcurementId,
            Label(row.Spmp, row.JobName),
            row.IsCancelled,
            FinanceTable.Fields.Keys.ToDictionary(field => field, field => Canonical(FinanceTable.Fields[field], Typed(row, field)))
        );

    /// <summary>A Docutrax job's LDP row as an import compares it, with the values the LDP export writes.</summary>
    public static FinanceImportJob FromLdp(LdpRecapDto item)
    {
        object? Value(FinanceField field) =>
            field switch
            {
                FinanceField.PrNumber => item.NoPr,
                FinanceField.NilaiAccrual => item.NilaiAccrual,
                FinanceField.NilaiRealisasi => item.NilaiRealisasi,
                FinanceField.PoNumber => item.NoPo,
                FinanceField.InproDate => item.TanggalSubmitIspa,
                FinanceField.HardcopyDate => item.TglSubmitHardcopy,
                FinanceField.PicAppo => item.PicApPo,
                FinanceField.InvoiceNumber => item.NoInvoice,
                FinanceField.InvoiceDate => item.TglInvoice,
                FinanceField.SaNo => item.SANo,
                FinanceField.SaDate => item.TglSa,
                FinanceField.Sp3No => item.SP3No,
                FinanceField.Sp3Date => item.TglSp3,
                FinanceField.LoketDate => item.TglSubmitLoket,
                FinanceField.PicApInvoice => item.PicApInvoice,
                FinanceField.NoAccrual => item.NoAccrual,
                _ => null,
            };

        return new(
            item.ProcurementId,
            Label(item.NoSpmp, item.TextPekerjaan),
            item.IsCancelled,
            LdpColumns.Values.ToDictionary(field => field, field => Canonical(FinanceTable.Fields[field], Value(field)))
        );
    }

    private static string Label(string? spmp, string? job) => $"{FinanceTable.ShortSpmp(spmp) ?? "-"} · {job ?? "-"}";

    /// <summary>
    /// Reads a cell the way the table's input reads it. The value comes back in the form the field's
    /// save takes (a date as yyyy-MM-dd, an amount with a decimal comma), or null for an empty cell.
    /// </summary>
    public static string? Normalize(FinanceFieldSpec spec, object? cell, out string? value)
    {
        value = null;
        if (cell is null || cell is string blank && string.IsNullOrWhiteSpace(blank))
            return null;

        switch (spec.Input)
        {
            case FinanceFieldInput.Date:
            {
                if (ReadDate(cell) is not { } date)
                    return $"Tanggal {spec.Label} tidak valid: \"{Show(cell)}\".";
                value = date.ToString("yyyy-MM-dd", Invariant);
                return null;
            }
            case FinanceFieldInput.Money:
            {
                decimal amount;
                if (cell is decimal or double or int or long)
                    amount = Convert.ToDecimal(cell, Invariant);
                else if (!FinanceTable.TryParseMoney(Show(cell), out amount))
                    return $"{spec.Label} harus berupa angka, misalnya 1.500.000.";
                if (amount < 0)
                    return $"{spec.Label} tidak boleh negatif.";
                value = Money(amount);
                return null;
            }
            default:
            {
                var text = Show(cell);
                if (text.Length > spec.MaxLength)
                    return $"{spec.Label} maksimal {spec.MaxLength} karakter.";
                value = text.Length == 0 ? null : text;
                return null;
            }
        }
    }

    // The value a job has now, in the form Normalize gives a cell, so equal values compare equal.
    private static string? Canonical(FinanceFieldSpec spec, object? value) =>
        value switch
        {
            null => null,
            DateTime date => date.ToString("yyyy-MM-dd", Invariant),
            decimal amount => Money(amount),
            string text => string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
            _ => Show(value),
        };

    private static string Money(decimal amount) => amount.ToString("0.##", Indonesian);

    // A number read from Excel is written in full, never as 4.5E+09.
    private static string Show(object cell) =>
        cell switch
        {
            decimal number => number.ToString("0.############", Invariant),
            double number => ((decimal)number).ToString("0.############", Invariant),
            DateTime date => date.ToString("yyyy-MM-dd", Invariant),
            _ => cell.ToString()?.Trim() ?? string.Empty,
        };

    private static DateTime? ReadDate(object cell)
    {
        if (cell is DateTime date)
            return date.Date;
        if (cell is double or decimal)
        {
            var serial = Convert.ToDouble(cell, Invariant);
            return serial is >= 1 and < 2958466 ? DateTime.FromOADate(serial).Date : null;
        }

        var text = Show(cell);
        foreach (var culture in new[] { Indonesian, Invariant })
        {
            if (DateTime.TryParseExact(text, DateFormats, culture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
                return parsed.Date;
        }

        return null;
    }

    private static string Display(FinanceFieldSpec spec, string? value) =>
        value is null ? "-"
        : spec.Input == FinanceFieldInput.Date && DateTime.TryParseExact(value, "yyyy-MM-dd", Invariant, DateTimeStyles.None, out var date) ? FinanceTable.FormatDate(date)
        : spec.Input == FinanceFieldInput.Money && FinanceTable.TryParseMoney(value, out var amount) ? FinanceTable.FormatMoney(amount)
        : value;

    /// <summary>What importing <paramref name="sheet"/> would change, without changing anything.</summary>
    /// <param name="jobs">The jobs the list holds, by ID Docutrax.</param>
    /// <exception cref="FinanceImportException">The sheet is not an export of this list.</exception>
    public static FinanceImportPlan Plan(FinanceImportTarget target, ExcelSheet sheet, IReadOnlyDictionary<string, FinanceImportJob> jobs)
    {
        var headers = sheet.Headers.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!headers.Contains(IdHeader))
            throw new FinanceImportException($"Kolom {IdHeader} tidak ditemukan. Pakai file hasil Ekspor Excel dari menu ini.");

        var columns = Columns(target).Where(column => headers.Contains(column.Key)).ToList();
        if (columns.Count == 0)
            throw new FinanceImportException("Tidak ada kolom isian yang dikenali. Pakai file hasil Ekspor Excel dari menu ini.");

        var notFound = target == FinanceImportTarget.Ldp
            ? "Bukan pekerjaan Docutrax. Baris Arsip LDP diperbarui lewat menu Arsip LDP."
            : "Pekerjaan tidak ada di tabel ini, atau sudah dihapus.";

        var changes = new List<FinanceImportChange>();
        var rejected = new List<FinanceImportRejection>();
        var unchanged = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in sheet.Rows)
        {
            var id = row.Cells.GetValueOrDefault(IdHeader) is { } idCell ? Show(idCell) : string.Empty;
            if (id.Length == 0)
            {
                rejected.Add(new(row.Number, "-", $"{IdHeader} kosong."));
                continue;
            }
            if (!seen.Add(id))
            {
                rejected.Add(new(row.Number, id, $"{IdHeader} muncul lebih dari sekali; baris ini dilewati."));
                continue;
            }
            if (!jobs.TryGetValue(id, out var job))
            {
                rejected.Add(new(row.Number, id, notFound));
                continue;
            }

            var rowChanges = new List<FinanceImportChange>();
            string? error = null;
            foreach (var (header, field) in columns)
            {
                var spec = FinanceTable.Fields[field];
                error = Normalize(spec, row.Cells.GetValueOrDefault(header), out var value);
                if (error is not null)
                    break;

                var now = job.Values.GetValueOrDefault(field);
                if (string.Equals(value, now, StringComparison.Ordinal))
                    continue;

                rowChanges.Add(new(job.ProcurementId, job.Label, field, spec.Label, Display(spec, now), value is null ? "dikosongkan" : Display(spec, value), value));
            }

            if (error is not null)
                rejected.Add(new(row.Number, job.Label, error));
            else if (rowChanges.Count == 0)
                unchanged++;
            else if (job.IsCancelled)
                rejected.Add(new(row.Number, job.Label, "Pekerjaan yang sudah di-cancel tidak bisa diubah."));
            else
                changes.AddRange(rowChanges);
        }

        return new FinanceImportPlan(target, changes, unchanged, rejected);
    }
}
