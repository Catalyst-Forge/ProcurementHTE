using System.Globalization;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using MigraDocCore.Rendering;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Services;

public partial class PdfGeneratorService : IPdfGenerator
{
    private const double PageWidthMm = 180; // konten maks di dalam margin
    private static readonly CultureInfo Id = new("id-ID");

    public Task<byte[]> GenerateProfitLossPdfAsync(
        ProfitLoss pnl,
        Procurement Procurement,
        Vendor? selectedVendor,
        IReadOnlyList<VendorOffer> offers
    )
    {
        var doc = new Document();
        DefineStyles(doc);

        var section = doc.AddSection();
        section.PageSetup = doc.DefaultPageSetup.Clone();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromMillimeter(15);
        section.PageSetup.BottomMargin = Unit.FromMillimeter(15);
        section.PageSetup.LeftMargin = Unit.FromMillimeter(15);
        section.PageSetup.RightMargin = Unit.FromMillimeter(15);

        // ======= HEADER =======
        AddHeader(section, Procurement, pnl);

        // ======= TABEL VENDOR TERPILIH / PESERTA =======
        AddVendorsTable(section, offers, selectedVendor);

        // ======= TAGIHAN KE PDC (ringkas) =======
        AddChargesTable(section, pnl);

        // ======= TABEL PENAWARAN PER VENDOR (round/nego) =======
        AddOffersTable(section, offers);

        // ======= PNL SUMMARY (Revenue, Cost, Profit, % dll) =======
        AddPnLSummary(section, pnl, selectedVendor);

        // ======= BARIS TANDA TANGAN =======
        AddSignatures(section);

        // ======= CATATAN =======
        AddNotes(section, selectedVendor, pnl);

        var renderer = new PdfDocumentRenderer(unicode: true) { Document = doc };
        renderer.RenderDocument();
        using var ms = new MemoryStream();
        renderer.PdfDocument.Save(ms);
        return Task.FromResult(ms.ToArray());
    }

    private static void AddHeader(Section s, Procurement Procurement, ProfitLoss pnl)
    {
        var title = s.AddParagraph($"Pengangkutan / P&L � Procurement {Procurement.ProcNum}");
        title.Style = "Heading1";
        title.Format.SpaceAfter = Unit.FromMillimeter(2);

        // info box kanan
        var tbl = s.AddTable();
        tbl.Borders.Width = 0.5;
        tbl.Format.SpaceAfter = Unit.FromMillimeter(4);

        // 2 kolom: label 35mm, value sisa
        var c1 = tbl.AddColumn(Unit.FromMillimeter(40));
        var c2 = tbl.AddColumn(Unit.FromMillimeter(PageWidthMm - 40));

        void Row(string label, string value)
        {
            var r = tbl.AddRow();
            r.TopPadding = 1;
            r.BottomPadding = 1;
            r.Cells[0].AddParagraph(label).Format.Font.Bold = true;
            r.Cells[0].Shading.Color = Colors.WhiteSmoke;
            r.Cells[1].AddParagraph(value);
        }

        Row("No Procurement", Procurement.ProcNum ?? "-");
        Row("Tanggal Procurement", (Procurement.CreatedAt).ToString("dd/MM/yyyy", Id) ?? "-");
        Row("Status Pekerjaan", Procurement.Status?.StatusName ?? "-");
        Row("Tanggal P&L", pnl.CreatedAt.ToString("dd/MM/yyyy", Id));
    }

    private static void AddVendorsTable(
        Section s,
        IReadOnlyList<VendorOffer> offers,
        Vendor? selected
    )
    {
        var box = s.AddParagraph("Vendor yang berpartisipasi / dipertimbangkan");
        box.Style = "Heading2";
        box.Format.SpaceAfter = Unit.FromMillimeter(2);

        var tbl = s.AddTable();
        tbl.Borders.Width = 0.5;
        tbl.Rows.LeftIndent = 0;
        tbl.Format.SpaceAfter = Unit.FromMillimeter(4);

        // 4 kolom: No, Vendor, Penawaran Terakhir, Keterangan
        var widths = new[] { 12.0, 70.0, 40.0, 58.0 }; // total = 180mm
        foreach (var w in widths)
            tbl.AddColumn(Unit.FromMillimeter(w));

        var hdr = tbl.AddRow();
        Header(hdr, "No", "Vendor", "Harga Akhir", "Keterangan");

        var vendorGroups = offers
            .GroupBy(o => new { o.VendorId, Name = o.Vendor?.VendorName ?? "-" })
            .OrderBy(g => g.Key.Name)
            .ToList();

        int i = 1;
        foreach (var g in vendorGroups)
        {
            var last = g.OrderByDescending(x => x.Round)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefault();
            var row = tbl.AddRow();
            row.Cells[0].AddParagraph(i.ToString());
            row.Cells[1]
                .AddParagraph(
                    g.Key.Name + (selected?.VendorId == g.Key.VendorId ? "  (TERPILIH)" : "")
                );
            row.Cells[2].AddParagraph(last != null ? Rp(last.Price) : "-");
            row.Cells[3]
                .AddParagraph(
                    selected?.VendorId == g.Key.VendorId ? "Best Value / Direkomendasikan" : "-"
                );
            i++;
        }
    }
}
