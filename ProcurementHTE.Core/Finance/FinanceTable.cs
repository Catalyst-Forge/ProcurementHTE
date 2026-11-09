using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Finance;

/// <summary>The finance menus that fill a job's cells without picking it up first.</summary>
public enum FinanceTableKind
{
    ApPo,
    ApInvoice,
    Ar,
    Accrual,
}

/// <summary>A cell a finance role fills by clicking it.</summary>
public enum FinanceField
{
    PrNumber,
    NilaiAccrual,
    NilaiRealisasi,
    PoNumber,
    InproDate,
    HardcopyDate,
    PicAppo,
    InvoiceNumber,
    InvoiceDate,
    SaNo,
    SaDate,
    Sp3No,
    Sp3Date,
    LoketDate,
    PicApInvoice,
    PicAr,
    NoAccrual,
}

public enum FinanceFieldInput
{
    Text,
    Date,
    Money,
}

public sealed record FinanceFieldSpec(
    FinanceField Field,
    FinanceTableKind Table,
    string Label,
    FinanceFieldInput Input,
    int MaxLength
);

public sealed record FinanceFieldChange(bool Success, string? Error, string Display);

/// <summary>One job as the AP-PO, AP-Invoice, AR, and Data Accrual tables show it.</summary>
public sealed record FinanceTableRow
{
    public required string ProcurementId { get; init; }
    public string? ProcNum { get; init; }
    public string? Spmp { get; init; }
    public string? JobName { get; init; }
    public string? Wonum { get; init; }
    public string? Vendor { get; init; }
    public string? JobType { get; init; }
    public string? StatusName { get; init; }
    public ProcurementStatus Stage { get; init; }
    public bool IsCancelled { get; init; }
    public bool HasProfitLoss { get; init; }
    public bool ApprovalAwaitingSend { get; init; }

    public string? PrNumber { get; init; }
    public decimal? NilaiPnl { get; init; }
    public decimal? NilaiAccrual { get; init; }
    public decimal? NilaiRealisasi { get; init; }
    public string? PoNumber { get; init; }
    public DateTime? InproDate { get; init; }
    public DateTime? HardcopyDate { get; init; }
    public string? PicAppo { get; init; }

    public bool RequiresRaNumber { get; init; }
    public bool CanChangeRaNumber { get; init; }
    public string? RaNumber { get; init; }

    public string? InvoiceNumber { get; init; }
    public DateTime? InvoiceDate { get; init; }
    public string? SaNo { get; init; }
    public DateTime? SaDate { get; init; }
    public string? Sp3No { get; init; }
    public DateTime? Sp3Date { get; init; }
    public DateTime? LoketDate { get; init; }
    public string? PicApInvoice { get; init; }

    public string? NoAccrual { get; init; }
    public decimal? PotensiAccrual { get; init; }
    public string? StatusAccrual { get; init; }
    public string? PicAr { get; init; }

    public bool ApPoDone =>
        Filled(PrNumber) && Filled(PoNumber) && InproDate.HasValue && HardcopyDate.HasValue && Filled(PicAppo);

    public bool ApInvoiceDone =>
        Filled(InvoiceNumber)
        && InvoiceDate.HasValue
        && Filled(SaNo)
        && SaDate.HasValue
        && Filled(Sp3No)
        && Sp3Date.HasValue
        && LoketDate.HasValue;

    public bool ArDone => Filled(NoAccrual) && Filled(PicAr);

    public bool AccrualDone => Filled(NoAccrual);

    /// <summary>
    /// The approver AP-PO sends the job to next, such as "Analyst HTE", or null when there is
    /// nothing to send: not published yet, already with an approver, or past the approvals.
    /// </summary>
    public string? NextApprovalTarget =>
        IsCancelled ? null
        : Stage == ProcurementStatus.OnCreateDP3 ? FinanceTable.ApproverFor(ProcurementStatus.WaitingApprovalAnalyst)
        : ApprovalAwaitingSend ? FinanceTable.ApproverFor(Stage)
        : null;

    /// <summary>The approver who has the job now, for the "Menunggu ..." label.</summary>
    public string? WaitingOn =>
        !IsCancelled && !ApprovalAwaitingSend ? FinanceTable.ApproverFor(Stage) : null;

    /// <summary>
    /// The first send needs a P&amp;L, since its value decides whether the VP signs.
    /// Documents may follow later.
    /// </summary>
    public bool CanSendApproval =>
        NextApprovalTarget != null && (Stage != ProcurementStatus.OnCreateDP3 || HasProfitLoss);

    /// <summary>A published job that cannot go to the Analyst yet because it has no P&amp;L.</summary>
    public bool NeedsProfitLossToSend => !IsCancelled && Stage == ProcurementStatus.OnCreateDP3 && !HasProfitLoss;

    private static bool Filled(string? value) => !string.IsNullOrWhiteSpace(value);
}

public static class FinanceTable
{
    /// <summary>Statuses a job can have once Operation has published it.</summary>
    public static readonly IReadOnlyList<string> PublishedStatuses =
    [
        "Waiting Pickup",
        "In Progress",
        "Completed",
        "Closed",
        "Cancelled",
    ];

    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    private static FinanceFieldSpec Spec(FinanceField field, FinanceTableKind table, string label, FinanceFieldInput input, int max = 0) =>
        new(field, table, label, input, max);

    public static readonly IReadOnlyDictionary<FinanceField, FinanceFieldSpec> Fields = new[]
    {
        Spec(FinanceField.PrNumber, FinanceTableKind.ApPo, "No PR", FinanceFieldInput.Text, 100),
        Spec(FinanceField.NilaiAccrual, FinanceTableKind.ApPo, "Nilai Accrual", FinanceFieldInput.Money),
        Spec(FinanceField.NilaiRealisasi, FinanceTableKind.ApPo, "Nilai Realisasi", FinanceFieldInput.Money),
        Spec(FinanceField.PoNumber, FinanceTableKind.ApPo, "No PO", FinanceFieldInput.Text, 100),
        Spec(FinanceField.InproDate, FinanceTableKind.ApPo, "Submit INPRO", FinanceFieldInput.Date),
        Spec(FinanceField.HardcopyDate, FinanceTableKind.ApPo, "Submit Hard Copy", FinanceFieldInput.Date),
        Spec(FinanceField.PicAppo, FinanceTableKind.ApPo, "PIC APPO", FinanceFieldInput.Text, 150),
        Spec(FinanceField.InvoiceNumber, FinanceTableKind.ApInvoice, "No Invoice", FinanceFieldInput.Text, 100),
        Spec(FinanceField.InvoiceDate, FinanceTableKind.ApInvoice, "Tgl Invoice", FinanceFieldInput.Date),
        Spec(FinanceField.SaNo, FinanceTableKind.ApInvoice, "No SA", FinanceFieldInput.Text, 100),
        Spec(FinanceField.SaDate, FinanceTableKind.ApInvoice, "Tgl SA", FinanceFieldInput.Date),
        Spec(FinanceField.Sp3No, FinanceTableKind.ApInvoice, "No SP3", FinanceFieldInput.Text, 100),
        Spec(FinanceField.Sp3Date, FinanceTableKind.ApInvoice, "Tgl SP3", FinanceFieldInput.Date),
        Spec(FinanceField.LoketDate, FinanceTableKind.ApInvoice, "Submit Loket", FinanceFieldInput.Date),
        Spec(FinanceField.PicApInvoice, FinanceTableKind.ApInvoice, "PIC AP-Invoice", FinanceFieldInput.Text, 150),
        Spec(FinanceField.PicAr, FinanceTableKind.Ar, "PIC AR", FinanceFieldInput.Text, 150),
        Spec(FinanceField.NoAccrual, FinanceTableKind.Accrual, "No Accrual", FinanceFieldInput.Text, 100),
    }.ToDictionary(spec => spec.Field);

    public static FinanceTableRow ToRow(Procurement p)
    {
        var pnl = p.ProfitLosses?.Where(pl => !pl.IsDeleted).OrderByDescending(pl => pl.CreatedAt).FirstOrDefault();
        return new FinanceTableRow
        {
            ProcurementId = p.ProcurementId,
            ProcNum = p.ProcNum,
            Spmp = p.SpmpNumber,
            JobName = p.JobName,
            Wonum = p.Wonum,
            Vendor = pnl?.SelectedVendor?.VendorName,
            JobType = p.JobType?.TypeName,
            StatusName = p.Status?.StatusName,
            Stage = p.ProcurementStatus,
            IsCancelled = IsCancelled(p),
            HasProfitLoss = pnl != null,
            ApprovalAwaitingSend = p.ApprovalAwaitingSend && IsApprovalStage(p.ProcurementStatus),
            PrNumber = p.PrNumber,
            NilaiPnl = pnl?.Profit,
            // Typed by AP-PO; older jobs still show what their P&L held.
            NilaiAccrual = p.NilaiAccrual ?? pnl?.AccrualAmount,
            NilaiRealisasi = p.NilaiRealisasi ?? pnl?.RealizationAmount,
            PoNumber = p.PoNumber,
            InproDate = p.IspaSubmitDate,
            HardcopyDate = p.HardcopySubmittedAt,
            PicAppo = p.PicAppo,
            RequiresRaNumber = p.RequiresRaNumber,
            CanChangeRaNumber = p.CanChangeRaNumber && !IsCancelled(p),
            RaNumber = p.RaNumber,
            InvoiceNumber = p.InvoiceNumber,
            InvoiceDate = p.InvoiceDate,
            SaNo = p.SANo,
            SaDate = p.SaDate,
            Sp3No = p.SP3No,
            Sp3Date = p.Sp3Date,
            LoketDate = p.LoketSubmitDate,
            PicApInvoice = p.PicApInvoice,
            NoAccrual = p.NoAccrual,
            PotensiAccrual = p.PotensiAccrual,
            StatusAccrual = p.StatusAccrual,
            PicAr = p.PicAr,
        };
    }

    /// <summary>The P&amp;L decides whether approval reaches the VP, so approval waits for it.</summary>
    public static bool HasProfitLoss(Procurement p) => p.ProfitLosses?.Any(pl => !pl.IsDeleted) == true;

    public static bool IsCancelled(Procurement p) =>
        p.ProcurementStatus == ProcurementStatus.Cancelled
        || string.Equals(p.Status?.StatusName, "Cancelled", StringComparison.OrdinalIgnoreCase);

    public static bool IsApprovalStage(ProcurementStatus stage) => ApproverFor(stage) != null;

    /// <summary>Who signs at an approval step. Approval stops at the VP.</summary>
    public static string? ApproverFor(ProcurementStatus stage) =>
        stage switch
        {
            ProcurementStatus.WaitingApprovalAnalyst => "Analyst HTE",
            ProcurementStatus.WaitingApprovalAsstManager => "Asst. Manager",
            ProcurementStatus.WaitingApprovalManager => "Manager",
            ProcurementStatus.WaitingApprovalVP => "Vice President",
            _ => null,
        };

    /// <summary>
    /// Sets one cell from what the user typed. Blank clears it. A date comes as yyyy-MM-dd,
    /// the value of an &lt;input type="date"&gt;. Money accepts 1.500.000, 1500000, or Rp 1.500.000.
    /// </summary>
    public static FinanceFieldChange Apply(Procurement p, FinanceField field, string? raw)
    {
        if (IsCancelled(p))
            return new(false, "Pekerjaan yang sudah di-cancel tidak bisa diubah.", string.Empty);

        var spec = Fields[field];
        var value = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();

        switch (spec.Input)
        {
            case FinanceFieldInput.Date:
            {
                DateTime? date = null;
                if (value != null)
                {
                    if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                        return new(false, $"Tanggal {spec.Label} tidak valid.", string.Empty);
                    date = parsed.Date;
                }

                switch (field)
                {
                    case FinanceField.InproDate: p.IspaSubmitDate = date; break;
                    case FinanceField.HardcopyDate: p.HardcopySubmittedAt = date; break;
                    case FinanceField.InvoiceDate: p.InvoiceDate = date; break;
                    case FinanceField.SaDate: p.SaDate = date; break;
                    case FinanceField.Sp3Date: p.Sp3Date = date; break;
                    case FinanceField.LoketDate: p.LoketSubmitDate = date; break;
                }
                return new(true, null, FormatDate(date));
            }

            case FinanceFieldInput.Money:
            {
                decimal? amount = null;
                if (value != null)
                {
                    if (!TryParseMoney(value, out var parsed))
                        return new(false, $"{spec.Label} harus berupa angka, misalnya 1.500.000.", string.Empty);
                    if (parsed < 0)
                        return new(false, $"{spec.Label} tidak boleh negatif.", string.Empty);
                    amount = parsed;
                }

                if (field == FinanceField.NilaiAccrual)
                    p.NilaiAccrual = amount;
                else
                    p.NilaiRealisasi = amount;
                return new(true, null, FormatMoney(amount));
            }
        }

        if (value != null && value.Length > spec.MaxLength)
            return new(false, $"{spec.Label} maksimal {spec.MaxLength} karakter.", string.Empty);

        switch (field)
        {
            case FinanceField.PrNumber: p.PrNumber = value; break;
            case FinanceField.PoNumber: p.PoNumber = value; break;
            case FinanceField.PicAppo: p.PicAppo = value; break;
            case FinanceField.InvoiceNumber: p.InvoiceNumber = value; break;
            case FinanceField.SaNo: p.SANo = value; break;
            case FinanceField.Sp3No: p.SP3No = value; break;
            case FinanceField.PicApInvoice: p.PicApInvoice = value; break;
            case FinanceField.PicAr: p.PicAr = value; break;
            case FinanceField.NoAccrual: p.NoAccrual = value; break;
        }

        return new(true, null, value ?? "-");
    }

    /// <summary>Reads an amount typed the Indonesian way: dots group thousands, a comma marks decimals.</summary>
    public static bool TryParseMoney(string text, out decimal amount)
    {
        var plain = text.Replace("Rp", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(" ", string.Empty).Trim();
        if (System.Text.RegularExpressions.Regex.IsMatch(plain, @"^-?\d{1,3}(\.\d{3})*(,\d+)?$|^-?\d+(,\d+)?$"))
            return decimal.TryParse(plain.Replace(".", string.Empty).Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

        amount = 0;
        return false;
    }

    /// <summary>
    /// Once every approval is in, the INPRO, Hard Copy, and PO steps follow the cells instead of a
    /// fixed order: the first empty one is the current step, and all three filled is Done PO.
    /// Returns null while the job is still before or outside that stretch.
    /// </summary>
    public static ProcurementStatus? StageAfterApproval(Procurement p)
    {
        if (
            p.ProcurementStatus
            is not (
                ProcurementStatus.OnSubmitISPA
                or ProcurementStatus.OnSubmitHardcopy
                or ProcurementStatus.OnSubmitPO
                or ProcurementStatus.DonePO
            )
        )
            return null;

        if (!p.IspaSubmitDate.HasValue)
            return ProcurementStatus.OnSubmitISPA;
        if (!p.HardcopySubmittedAt.HasValue)
            return ProcurementStatus.OnSubmitHardcopy;
        if (string.IsNullOrWhiteSpace(p.PoNumber))
            return ProcurementStatus.OnSubmitPO;
        return ProcurementStatus.DonePO;
    }

    /// <summary>The step name the tracker shows, such as "Waiting Approval Manager".</summary>
    public static string StageLabel(ProcurementStatus stage) =>
        typeof(ProcurementStatus)
            .GetField(stage.ToString())
            ?.GetCustomAttribute<DescriptionAttribute>()
            ?.Description ?? stage.ToString();

    /// <summary>Keys the tables may use in sort-key and filter-key.</summary>
    public static IReadOnlyCollection<string> ColumnKeys => Columns.Keys.ToList();

    public static string FormatDate(DateTime? date) => date?.ToString("dd MMM yyyy", Id) ?? "-";

    public static string FormatMoney(decimal? value) => value.HasValue ? value.Value.ToString("N0", Id) : "-";

    // Column keys match <th sort-key> in the views and the f.key filters from the column menu.
    private static readonly IReadOnlyDictionary<string, Func<FinanceTableRow, object?>> Columns =
        new Dictionary<string, Func<FinanceTableRow, object?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["procNum"] = r => r.ProcNum,
            ["spmp"] = r => r.Spmp,
            ["jobName"] = r => r.JobName,
            ["wonum"] = r => r.Wonum,
            ["vendor"] = r => r.Vendor,
            ["prNumber"] = r => r.PrNumber,
            ["nilaiPnl"] = r => r.NilaiPnl,
            ["nilaiAccrual"] = r => r.NilaiAccrual,
            ["nilaiRealisasi"] = r => r.NilaiRealisasi,
            ["poNumber"] = r => r.PoNumber,
            ["inpro"] = r => r.InproDate,
            ["hardcopy"] = r => r.HardcopyDate,
            ["picAppo"] = r => r.PicAppo,
            ["apPoStatus"] = r => r.IsCancelled ? "Cancel" : r.ApPoDone ? "Done" : "On Progress",
            ["invoiceNumber"] = r => r.InvoiceNumber,
            ["invoiceDate"] = r => r.InvoiceDate,
            ["saNo"] = r => r.SaNo,
            ["saDate"] = r => r.SaDate,
            ["sp3No"] = r => r.Sp3No,
            ["sp3Date"] = r => r.Sp3Date,
            ["loket"] = r => r.LoketDate,
            ["picApInvoice"] = r => r.PicApInvoice,
            ["apInvoiceStatus"] = r => r.IsCancelled ? "Cancel" : r.ApInvoiceDone ? "Done" : "On Progress",
            ["accrualTableStatus"] = r => r.IsCancelled ? "Cancel" : r.AccrualDone ? "Done" : "On Progress",
            ["noAccrual"] = r => r.NoAccrual,
            ["potensiAccrual"] = r => r.PotensiAccrual,
            ["statusAccrual"] = r => r.StatusAccrual,
            ["picAr"] = r => r.PicAr,
            ["arStatus"] = r => r.IsCancelled ? "Cancel" : r.ArDone ? "Done" : "On Progress",
        };

    /// <summary>Search across the visible text, per-column filters, sort, and the page.</summary>
    public static PagedResult<FinanceTableRow> Query(
        IEnumerable<FinanceTableRow> rows,
        string? search,
        IReadOnlyDictionary<string, string> filters,
        ListSort? sort,
        int page,
        int pageSize
    )
    {
        IEnumerable<FinanceTableRow> query = rows;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(row => Columns.Values.Any(column => Contains(Text(column(row)), term)));
        }

        foreach (var (key, value) in filters)
        {
            if (Columns.TryGetValue(key, out var column) && !string.IsNullOrWhiteSpace(value))
                query = query.Where(row => Contains(Text(column(row)), value.Trim()));
        }

        if (sort != null && Columns.TryGetValue(sort.Key, out var sortColumn))
        {
            // Empty cells go last whichever way the column is sorted.
            var ordered = query.OrderBy(row => sortColumn(row) is null);
            query = sort.Descending
                ? ordered.ThenByDescending(row => sortColumn(row), Comparer)
                : ordered.ThenBy(row => sortColumn(row), Comparer);
        }

        var all = query.ToList();
        var size = pageSize > 0 ? pageSize : 10;
        var lastPage = Math.Max(1, (int)Math.Ceiling(all.Count / (double)size));
        var current = Math.Clamp(page, 1, lastPage);

        return new PagedResult<FinanceTableRow>
        {
            Items = all.Skip((current - 1) * size).Take(size).ToList(),
            Page = current,
            PageSize = size,
            Total = all.Count,
        };
    }

    private static readonly Comparer<object?> Comparer = Comparer<object?>.Create(
        (a, b) =>
            (a, b) switch
            {
                (null, null) => 0,
                (null, _) => 1,
                (_, null) => -1,
                (IComparable x, _) when x.GetType() == b.GetType() => x.CompareTo(b),
                _ => string.Compare(Text(a), Text(b), StringComparison.OrdinalIgnoreCase),
            }
    );

    private static string Text(object? value) =>
        value switch
        {
            null => string.Empty,
            DateTime date => FormatDate(date),
            decimal number => FormatMoney(number),
            _ => value.ToString() ?? string.Empty,
        };

    private static bool Contains(string text, string term) =>
        text.Contains(term, StringComparison.OrdinalIgnoreCase);
}
