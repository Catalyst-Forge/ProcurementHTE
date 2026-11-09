using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels;

/// <summary>The Excel import page of AP-PO, AP-Invoice, Data Accrual, or the LDP: upload, preview, import.</summary>
public sealed class FinanceImportPageViewModel
{
    public required FinanceImportTarget Target { get; init; }
    public required string Title { get; init; }
    public required string ListLabel { get; init; }
    public required string Columns { get; init; }
    public string? Token { get; set; }
    public string? FileName { get; set; }
    public FinanceImportPlan? Plan { get; set; }

    public static FinanceImportPageViewModel For(FinanceImportTarget target) =>
        target switch
        {
            FinanceImportTarget.ApPo => new() { Target = target, Title = "Impor Excel ke AP Purchase Order", ListLabel = "AP Purchase Order", Columns = "No PR, Nilai Accrual, Nilai Realisasi, No PO, Submit INPRO, Submit Hard Copy, dan PIC APPO. No RA tetap diisi lewat tabel." },
            FinanceImportTarget.ApInvoice => new() { Target = target, Title = "Impor Excel ke AP Invoice", ListLabel = "AP Invoice", Columns = "No Invoice, Tgl Invoice, No SA, Tgl SA, No SP3, Tgl SP3, Submit Loket, dan PIC AP-Invoice." },
            FinanceImportTarget.Accrual => new() { Target = target, Title = "Impor Excel ke Data Accrual", ListLabel = "Data Accrual", Columns = "No Accrual." },
            _ => new() { Target = target, Title = "Impor Excel ke LDP", ListLabel = "LDP", Columns = "isian keuangan pekerjaan Docutrax: No PR, Nilai Accrual, Nilai Realisasi, No PO, Tgl Submit INPRO, Submit Hard Copy, PIC AP-PO, No Invoice, Tgl Invoice, SA No, Tgl SA, SP3 No, Tgl SP3, Submit Loket, PIC AP-Invoice, dan No Accrual. Kolom lain dan baris Arsip LDP tidak diubah." },
        };
}
