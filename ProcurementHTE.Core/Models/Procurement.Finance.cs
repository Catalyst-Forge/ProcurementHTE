using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProcurementHTE.Core.Models;

/// <summary>
/// What the finance tables (AP-PO, AP-Invoice, AR) fill in by clicking a cell. The names are typed,
/// not picked from users, so a new or departed staff member never blocks the table.
/// </summary>
public partial class Procurement
{
    [MaxLength(100)]
    [DisplayName("No PR")]
    public string? PrNumber { get; set; }

    [MaxLength(150)]
    [DisplayName("PIC APPO")]
    public string? PicAppo { get; set; }

    [MaxLength(150)]
    [DisplayName("PIC AP-Invoice")]
    public string? PicApInvoice { get; set; }

    [MaxLength(150)]
    [DisplayName("PIC AR")]
    public string? PicAr { get; set; }

    // AP-PO types these in its table; the P&L form no longer asks for them.
    [Column(TypeName = "decimal(18, 2)")]
    [DisplayName("Nilai Accrual")]
    public decimal? NilaiAccrual { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [DisplayName("Nilai Realisasi")]
    public decimal? NilaiRealisasi { get; set; }

    // AP-Invoice
    [MaxLength(100)]
    [DisplayName("No Invoice")]
    public string? InvoiceNumber { get; set; }

    [DisplayName("Tgl Invoice")]
    public DateTime? InvoiceDate { get; set; }

    [DisplayName("Tgl SA")]
    public DateTime? SaDate { get; set; }

    [DisplayName("Tgl SP3")]
    public DateTime? Sp3Date { get; set; }

    [DisplayName("Submit Loket")]
    public DateTime? LoketSubmitDate { get; set; }

    /// <summary>
    /// An approver has signed and the job waits for AP-PO to send it to the next level.
    /// The next approver cannot act on it until then.
    /// </summary>
    public bool ApprovalAwaitingSend { get; set; }

    /// <summary>
    /// Only spot Angkutan jobs get their P&amp;L PDF made when the P&amp;L is saved. Every other job
    /// uploads its own, and a generated file can always be replaced by an upload.
    /// </summary>
    public bool GeneratesProfitLossDocument =>
        ContractType == Enums.ContractType.Spot
        && string.Equals(JobType?.TypeName, "Angkutan", StringComparison.OrdinalIgnoreCase);
}
