using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Services;

public partial class PdfGeneratorService
{
    private static void AddChargesTable(Section s, ProfitLoss pnl)
    {
        var title = s.AddParagraph("Tagihan ke PDC");
        title.Style = "Heading2";
        title.Format.SpaceAfter = Unit.FromMillimeter(1.5);

        var tbl = s.AddTable();
        tbl.Borders.Width = 0.5;
        tbl.Format.SpaceAfter = Unit.FromMillimeter(4);

        // Kolom: Unit(20) | Tarif Awal(40) | Tarif Add(30) | Km/25(20) | Nilai Tambahan(35) | Jumlah(35) = 180
        double[] widths = { 20, 40, 30, 20, 35, 35 };
        foreach (var w in widths)
            tbl.AddColumn(Unit.FromMillimeter(w));

        var hdr = tbl.AddRow();
        Header(hdr, "Unit", "Tarif awal", "Tarif Add", "Km per 25", "Nilai Penambahan", "Jumlah");

        var r = tbl.AddRow();
        r.Cells[0].AddParagraph("1 (Highbed)"); // contoh ringkas

        // total bar
        var total = tbl.AddRow();
        total.Shading.Color = Colors.WhiteSmoke;
        total.Cells[0].MergeRight = 4;
        total.Cells[0].AddParagraph("Total Tagihan");
    }

    private static void AddOffersTable(Section s, IReadOnlyList<VendorOffer> offers)
    {
        var title = s.AddParagraph("Rincian Penawaran / Nego per Vendor");
        title.Style = "Heading2";
        title.Format.SpaceAfter = Unit.FromMillimeter(1.5);

        var tbl = s.AddTable();
        tbl.Borders.Width = 0.5;
        tbl.Format.SpaceAfter = Unit.FromMillimeter(4);

        // Kolom: Vendor(70) | Round(20) | Harga(40) | Catatan(50) = 180
        double[] widths = { 70, 20, 40, 50 };
        foreach (var w in widths)
            tbl.AddColumn(Unit.FromMillimeter(w));

        var hdr = tbl.AddRow();
        Header(hdr, "Vendor", "Round", "Harga", "Catatan");

        var rows = offers
            .OrderBy(o => o.Vendor?.VendorName ?? o.VendorId)
            .ThenBy(o => o.Round)
            .ToList();

        foreach (var o in rows)
        {
            var r = tbl.AddRow();
            r.Cells[0].AddParagraph(o.Vendor?.VendorName ?? "-");
            r.Cells[1].AddParagraph(o.Round.ToString());
            r.Cells[2].AddParagraph(Rp(o.Price));
            r.Cells[3].AddParagraph("Note");
        }
    }

    private static void AddPnLSummary(Section s, ProfitLoss pnl, Vendor? selectedVendor)
    {
        var title = s.AddParagraph("Profit & Loss Estimate");
        title.Style = "Heading2";
        title.Format.SpaceAfter = Unit.FromMillimeter(1.5);

        var tbl = s.AddTable();
        tbl.Borders.Width = 0.5;
        tbl.Format.SpaceAfter = Unit.FromMillimeter(4);

        // Kolom kiri label 80mm, kanan nilai 100mm
        tbl.AddColumn(Unit.FromMillimeter(80));
        tbl.AddColumn(Unit.FromMillimeter(100));

        Row2(
            tbl,
            $"Harga Mitra Terpilih{(selectedVendor != null ? $" — {selectedVendor.VendorName}" : "")}",
            Rp(pnl.SelectedVendorFinalOffer)
        );
        var profit = pnl.Profit;
        Row2(tbl, "PROFIT", Rp(profit), shaded: true, bold: true);

        // Baris % profit terhadap revenue
        var percent = pnl.ProfitPercent;
        Row2(tbl, "% Profit vs Revenue", $"{percent:0.##} %");
    }

    private static void AddSignatures(Section s)
    {
        var title = s.AddParagraph("Persetujuan");
        title.Style = "Heading2";
        title.Format.SpaceAfter = Unit.FromMillimeter(1.5);

        var tbl = s.AddTable();
        tbl.Borders.Width = 0.5;

        // 3 kolom tanda tangan @ 60mm = 180mm
        for (int i = 0; i < 3; i++)
            tbl.AddColumn(Unit.FromMillimeter(60));

        var rBox = tbl.AddRow();
        rBox.Height = Unit.FromMillimeter(25);
        rBox.VerticalAlignment = VerticalAlignment.Center;
        for (int i = 0; i < 3; i++)
        {
            rBox.Cells[i].AddParagraph("\n\n\n"); // ruang tanda tangan
        }

        var rCap = tbl.AddRow();
        rCap.Cells[0].AddParagraph("Disetujui oleh\nAnalyst HTE").Format.Alignment =
            ParagraphAlignment.Center;
        rCap.Cells[1].AddParagraph("Diketahui oleh\nAsisten Manager HTE").Format.Alignment =
            ParagraphAlignment.Center;
        rCap.Cells[2].AddParagraph("Disahkan oleh\nManager Transport & Logistic").Format.Alignment =
            ParagraphAlignment.Center;

        s.AddParagraph().Format.SpaceAfter = Unit.FromMillimeter(2);
    }

    private static void AddNotes(Section section, Vendor? selectedVendor, ProfitLoss pnl)
    {
        var heading = section.AddParagraph("Catatan:");
        heading.Format.Font.Bold = true;
        heading.Format.SpaceBefore = Unit.FromMillimeter(2);
        heading.Format.SpaceAfter = Unit.FromMillimeter(1);

        var baseInfo = new ListInfo
        {
            ListType = ListType.NumberList1,
            NumberPosition = Unit.FromMillimeter(6),
        };

        AddListItem(
            section,
            "Penawaran RFI melibatkan beberapa vendor terdaftar PDC.",
            baseInfo,
            first: true
        );
        AddListItem(
            section,
            $"Dari hasil negosiasi, vendor terpilih {selectedVendor?.VendorName ?? "-"}.",
            baseInfo
        );
        AddListItem(section, "Actual invoice disesuaikan dengan Surat Jalan.", baseInfo);
        AddListItem(
            section,
            "Mitra telah berpengalaman dan memiliki SKT & CSMS untuk pekerjaan ini.",
            baseInfo
        );

        // highlight ringkas profit
        var highlight = section.AddParagraph();
        highlight.Format.Shading.Color = Colors.LightYellow;
        highlight.Format.SpaceBefore = Unit.FromMillimeter(1);
        highlight.AddText($"Estimasi Profit: {Rp(pnl.Profit)} ({(pnl.ProfitPercent):0.##}%)");
    }
}
