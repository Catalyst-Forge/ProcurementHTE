using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Controllers.Ldp
{
    public partial class LdpController
    {
        // The columns of the LDP table, in its order.
        private static readonly (string Header, string Property)[] ExportColumns =
        [
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
    }
}
