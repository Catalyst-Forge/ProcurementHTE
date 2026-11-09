using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ProcurementHTE.Core.Finance;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Controllers.Finance;

namespace ProcurementHTE.Web.Controllers.Ldp
{
    public partial class LdpController
    {
        // The columns of the LDP table, in its order.
        /// <summary>
        /// The LDP export. ID Docutrax leads, so the file can be imported back; the fill-in columns use
        /// the headers FinanceImport.LdpColumns reads.
        /// </summary>
        public static readonly (string Header, string Property)[] ExportColumns =
        [
            (FinanceImport.IdHeader, nameof(LdpRecapDto.ProcurementId)),
            ("No WO", nameof(LdpRecapDto.NoWo)), ("No SPK", nameof(LdpRecapDto.NoSpk)),
            ("Vendor Terpilih", nameof(LdpRecapDto.NamaVendor)), ("Tgl Mulai", nameof(LdpRecapDto.TglMulai)),
            ("Tgl Selesai", nameof(LdpRecapDto.TglSelesai)), ("Job Name", nameof(LdpRecapDto.TextPekerjaan)),
            ("Job Type", nameof(LdpRecapDto.JobType)), ("Contract Type", nameof(LdpRecapDto.ContractType)),
            ("LTC Name", nameof(LdpRecapDto.LtcName)), ("Text Pekerjaan PR", nameof(LdpRecapDto.TextPekerjaanPr)),
            ("No Accrual", nameof(LdpRecapDto.NoAccrual)), ("Year Job", nameof(LdpRecapDto.YearJob)),
            ("No Rig", nameof(LdpRecapDto.NoRig)), ("Nilai PnL", nameof(LdpRecapDto.NilaiPnl)),
            ("Nilai Accrual", nameof(LdpRecapDto.NilaiAccrual)), ("Nilai Realisasi", nameof(LdpRecapDto.NilaiRealisasi)),
            ("No SPMP", nameof(LdpRecapDto.NoSpmp)), ("No HTE", nameof(LdpRecapDto.NoHte)),
            ("Project Region", nameof(LdpRecapDto.ProjectRegion)), ("Project Code", nameof(LdpRecapDto.ProjectCode)),
            ("Link Dokumen", nameof(LdpRecapDto.LinkDokumen)), ("Unit Item", nameof(LdpRecapDto.UnitItemPenawaran)),
            ("Surat Penawaran", nameof(LdpRecapDto.SuratPenawaranVendor)), ("Memorandum", nameof(LdpRecapDto.Memorandum)),
            ("Tgl Doc", nameof(LdpRecapDto.TglDoc)), ("Potensi Accrual", nameof(LdpRecapDto.PotensiAccrual)),
            ("Tgl Accrual", nameof(LdpRecapDto.TglAccrual)), ("Status Accrual", nameof(LdpRecapDto.StatusAccrual)),
            ("RKS Tgl Selesai", nameof(LdpRecapDto.RksTglSelesai)), ("BOQ Tgl Selesai", nameof(LdpRecapDto.BoqTglSelesai)),
            ("Memo Tgl Selesai", nameof(LdpRecapDto.MemoTglSelesai)), ("RA Tgl Selesai", nameof(LdpRecapDto.RaTglSelesai)),
            ("No PR", nameof(LdpRecapDto.NoPr)), ("Tgl Buat PR", nameof(LdpRecapDto.TanggalBuatPr)),
            ("Tgl Rilis PR", nameof(LdpRecapDto.TanggalRilisPr)), ("Approval Analyst", nameof(LdpRecapDto.TanggalApprovalAnalyst)),
            ("Approval Asst. Man", nameof(LdpRecapDto.TanggalApprovalAsstManager)), ("Approval Manager", nameof(LdpRecapDto.ManagerApprovalSelesai)),
            ("Approval VP", nameof(LdpRecapDto.VpApprovalSelesai)), ("Tgl Submit INPRO", nameof(LdpRecapDto.TanggalSubmitIspa)),
            ("SA No", nameof(LdpRecapDto.SANo)), ("SP3 No", nameof(LdpRecapDto.SP3No)), ("PIC AP-PO", nameof(LdpRecapDto.PicApPo)),
            ("No PO", nameof(LdpRecapDto.NoPo)), ("Submit Hard Copy", nameof(LdpRecapDto.TglSubmitHardcopy)),
            ("No Invoice", nameof(LdpRecapDto.NoInvoice)), ("Tgl Invoice", nameof(LdpRecapDto.TglInvoice)),
            ("Tgl SA", nameof(LdpRecapDto.TglSa)), ("Tgl SP3", nameof(LdpRecapDto.TglSp3)),
            ("Submit Loket", nameof(LdpRecapDto.TglSubmitLoket)), ("PIC AP-Invoice", nameof(LdpRecapDto.PicApInvoice)),
        ];

        [HttpGet]
        public async Task<IActionResult> Export(string? search = null, CancellationToken ct = default)
        {
            var items = await _ldpService.GetAllForExportAsync(search, ct);
            var properties = ExportColumns.Select(column => typeof(LdpRecapDto).GetProperty(column.Property)!).ToArray();
            var bytes = _excel.Export(
                "LDP",
                ExportColumns.Select(column => column.Header).ToList(),
                items.Select(item => (IReadOnlyList<object?>)properties.Select(property => property.GetValue(item)).ToList())
            );
            return File(bytes, ExcelContentType, $"LDP-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
        }

        internal const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        // An edited LDP export, imported back into the Docutrax jobs' fill-in fields. Admin only.
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Import() => FinanceImportEndpoints.Page(this, FinanceImportTarget.Ldp);

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        [RequestSizeLimit(FinanceImportEndpoints.MaxBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = FinanceImportEndpoints.MaxBytes)]
        public Task<IActionResult> ImportPreview(
            IFormFile? file,
            [FromServices] IExcelTableReader reader,
            [FromServices] IFinanceImportService imports,
            [FromServices] IMemoryCache cache,
            CancellationToken ct
        ) => FinanceImportEndpoints.PreviewAsync(this, FinanceImportTarget.Ldp, file, reader, imports, cache, ct);

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public Task<IActionResult> ImportApply(
            string? token,
            [FromServices] IFinanceImportService imports,
            [FromServices] IMemoryCache cache,
            CancellationToken ct
        ) => FinanceImportEndpoints.ApplyAsync(this, FinanceImportTarget.Ldp, token, imports, cache, ct);
    }
}
