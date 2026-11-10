using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Constants;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class LdpRepository
    {
        /// <summary>
        /// Archived jobs that started within the LDP range (an Admin setting), shaped as LDP rows.
        /// They show in the LDP itself, next to the jobs run in Docutrax.
        /// </summary>
        private async Task<List<LdpRecapDto>> PresentedArchiveAsync(string? search, CancellationToken ct)
        {
            var range = await LdpArchiveRangeRepository.CurrentAsync(_context, ct);
            var rows = await LdpArchiveRangeRepository
                .InRange(_context.LdpArchiveRows.AsNoTracking(), range)
                .ToListAsync(ct);

            var items = rows.Select(MapArchive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                items = items.Where(item =>
                    Contains(item.NoWo, term) || Contains(item.TextPekerjaan, term) || Contains(item.NoSpk, term)
                );
            }

            return items.ToList();
        }

        private static bool Contains(string? value, string term) =>
            value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;

        private static LdpRecapDto MapArchive(LdpArchiveRow row)
        {
            var cells = LdpArchiveEditor.Read(row.ValuesJson)
                .GroupBy(cell => cell.Label)
                .ToDictionary(group => group.Key, group => group.First().Value?.ToString());

            string? Text(string label) => cells.GetValueOrDefault(label);
            DateTime? Date(string label) =>
                DateTime.TryParse(Text(label), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
            decimal? Number(string label) =>
                decimal.TryParse(Text(label), NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;

            return new LdpRecapDto
            {
                ProcurementId = row.LdpArchiveRowId,
                ArchiveRowId = row.LdpArchiveRowId,
                NoWo = row.Wo,
                NoSpk = row.NoSpk,
                NamaVendor = row.VendorName,
                TglMulai = row.StartDate,
                TglSelesai = row.EndDate,
                TextPekerjaan = row.JobName,
                JobType = row.JobType,
                ContractType = row.ContractType,
                LtcName = row.LtcName,
                TextPekerjaanPr = Text("Text pekerjaan untuk Pr"),
                NoAccrual = Text("No Accrual"),
                YearJob = int.TryParse(row.YearJob, out var year) ? year : null,
                NoRig = Text("No Rig#"),
                NilaiPnl = row.NilaiPnl,
                NilaiAccrual = row.NilaiAccrual,
                NilaiRealisasi = row.NilaiRealisasi,
                NoSpmp = row.NoSpmp,
                ProjectRegion = Text("Project"),
                LinkDokumen = Text("Link Dokumen"),
                UnitItemPenawaran = Text("Unit"),
                SuratPenawaranVendor = Text("Surat Penwrn Vndr"),
                Memorandum = Text("Memo"),
                TglDoc = Date("Tgl. Doc"),
                PotensiAccrual = Number("Potensi Accr"),
                TglAccrual = Date("Tgl Accr"),
                StatusAccrual = Text("Stat Accr"),
                RksTglSelesai = Date("RKS,BOQ,MEMO · Selesai"),
                RaTglSelesai = Date("Doc (RA) · Selesai"),
                NoPr = row.NoPr,
                TanggalBuatPr = row.PrCreatedAt,
                TanggalRilisPr = Date("PR · Release"),
                ManagerApprovalSelesai = Date(LdpArchiveColumns.ManagerApprovalEnd),
                VpApprovalSelesai = Date("Aprvl VP · Selesai"),
                TanggalSubmitIspa = Date("Submit INPRO"),
                SANo = row.NoSa,
                SP3No = row.NoSp3,
                NoPo = row.NoPo,
                NoInvoice = row.NoInvoice,
                PicApPo = row.PicPr,
            };
        }
    }
}
