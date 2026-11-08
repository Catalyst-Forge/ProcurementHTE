namespace ProcurementHTE.Core.Constants;

/// <summary>
/// How the old LDP spreadsheet maps onto archive rows. The importer and the edit form
/// both read this, so a column moves in one place.
/// </summary>
public static class LdpArchiveColumns
{
    public const string Sheet = "LDP(new)";
    public const string CompletedStatus = "Completed";

    /// <summary>The spreadsheet writes "-" or 0 where a number is still missing.</summary>
    public static bool IsFilled(string? text) => !string.IsNullOrWhiteSpace(text) && text.Trim() is not ("-" or "0");

    // Jobs that started July to September 2026 are presented as completed, done or not.
    public static readonly DateTime PresentedFrom = new(2026, 7, 1);
    public static readonly DateTime PresentedTo = new(2026, 9, 30);

    public sealed record Column(string Property, string? Group, string? Name, string Kind)
    {
        /// <summary>The label a cell gets, with its group when the name alone repeats.</summary>
        public string Label => LabelFor(Group, Name);
    }

    public static string LabelFor(string? group, string? name) =>
        !string.IsNullOrEmpty(group) && !string.IsNullOrEmpty(name) && group != name
            ? $"{group} · {name}"
            : (name ?? group ?? string.Empty);

    public static readonly IReadOnlyList<Column> Typed =
    [
        new("Wo", null, "WO", "text"),
        new("NoSpk", null, "No. SPK", "text"),
        new("VendorName", null, "Nama Vendor", "text"),
        new("StartDate", null, "Tgl. Mulai", "date"),
        new("EndDate", null, "Tgl. Selesai", "date"),
        new("JobName", null, "Nama Pekerjaan", "text"),
        new("JobType", null, "Job Type", "text"),
        new("ContractType", null, "Cntract Type", "text"),
        new("LtcName", null, "LTC Name", "text"),
        new("YearJob", null, "Year Job", "text"),
        new("NilaiPnl", null, "Nilai P&L", "number"),
        new("NilaiAccrual", null, "Nilai Accrual", "number"),
        new("NilaiRealisasi", null, "Nilai Realisasi", "number"),
        new("NoSpmp", null, "No. SPMP", "text"),
        new("PrCreatedAt", "PR", "Create", "date"),
        new("NoPr", "PR", "No. PR", "text"),
        new("PoDate", "PO/RO", "Tgl", "date"),
        new("NoPo", "PO/RO", "No.PO", "text"),
        new("NoInvoice", "Tgl Process Inv Mitra", "No. Inv", "text"),
        new("InvoiceValue", "Tgl Process Inv Mitra", "Nilai (IDR)", "number"),
        new("NoSa", "Tgl Process Inv Mitra", "No. SA", "text"),
        new("NoSp3", "Tgl Process Inv Mitra", "No. SP3", "text"),
        new("PicPr", "Pic PR", null, "text"),
        new("PicOps", "Pic Ops", null, "text"),
        new("Keterangan", "Keterangan", null, "text"),
    ];

    public static readonly IReadOnlyDictionary<string, string> JobTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Angkt"] = "Angkutan",
        ["Stb"] = "StandBy",
        ["Mov"] = "Moving",
    };
}
