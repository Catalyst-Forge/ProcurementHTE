using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using MigraDocCore.DocumentObjectModel.Shapes;
using MigraDocCore.Rendering;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using System.Globalization;

namespace ProcurementHTE.Core.Services
{
    public class PdfGeneratorService : IPdfGenerator
    {
        private const double PageWidthMm = 180; // konten maks di dalam margin
        private static readonly CultureInfo Id = new("id-ID");

        public Task<byte[]> GenerateProfitLossPdfAsync(
            ProfitLoss pnl,
            WorkOrder wo,
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
            AddHeader(section, wo, pnl);

            // ======= TABEL VENDOR TERPILIH / PESERTA =======
            AddVendorsTable(section, offers, selectedVendor);

            // ======= TAGIHAN KE PDC (ringkas) =======
            AddChargesTable(section, pnl);

            // ======= TABEL PENAWARAN PER VENDOR (round/nego) =======
            AddOffersTable(section, offers);

            // ======= PNL SUMMARY (Revenue, Cost, Profit, % etc) =======
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

        private static void AddHeader(Section s, WorkOrder wo, ProfitLoss pnl) {
            var title = s.AddParagraph($"Pengangkutan / P&L — WO {wo.WoNum}");
            title.Style = "Heading1";
            title.Format.SpaceAfter = Unit.FromMillimeter(2);

            // info box kanan
            var tbl = s.AddTable();
            tbl.Borders.Width = 0.5;
            tbl.Format.SpaceAfter = Unit.FromMillimeter(4);

            // 2 kolom: label 35mm, value sisa
            var c1 = tbl.AddColumn(Unit.FromMillimeter(40));
            var c2 = tbl.AddColumn(Unit.FromMillimeter(PageWidthMm - 40));

            void Row(string label, string value) {
                var r = tbl.AddRow();
                r.TopPadding = 1;
                r.BottomPadding = 1;
                r.Cells[0].AddParagraph(label).Format.Font.Bold = true;
                r.Cells[0].Shading.Color = Colors.WhiteSmoke;
                r.Cells[1].AddParagraph(value);
            }

            Row("No WO", wo.WoNum ?? "-");
            Row("Tanggal WO", (wo.CreatedAt).ToString("dd/MM/yyyy", Id) ?? "-");
            Row("Status Pekerjaan", wo.Status?.StatusName ?? "-");
            Row("Tanggal P&L", pnl.CreatedAt.ToString("dd/MM/yyyy", Id));
        }

        private static void AddVendorsTable(Section s, IReadOnlyList<VendorOffer> offers, Vendor? selected) {
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
            foreach (var g in vendorGroups) {
                var last = g.OrderByDescending(x => x.Round).ThenByDescending(x => x.CreatedAt).FirstOrDefault();
                var row = tbl.AddRow();
                row.Cells[0].AddParagraph(i.ToString());
                row.Cells[1].AddParagraph(g.Key.Name + (selected?.VendorId == g.Key.VendorId ? "  (TERPILIH)" : ""));
                row.Cells[2].AddParagraph(last != null ? Rp(last.Price) : "-");
                row.Cells[3].AddParagraph(selected?.VendorId == g.Key.VendorId ? "Best Value / Direkomendasikan" : "-");
                i++;
            }
        }

        private static void AddChargesTable(Section s, ProfitLoss pnl) {
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
            //r.Cells[1].AddParagraph(Rp(pnl.TarifAwal));
            //r.Cells[2].AddParagraph(Rp(pnl.TarifAdd));
            //r.Cells[3].AddParagraph((pnl.KmPer25).ToString());
            //var addVal = (pnl.KmPer25) > 0 ? (pnl.TarifAdd) * (pnl.KmPer25) : 0;
            //r.Cells[4].AddParagraph(Rp(addVal));
            //r.Cells[5].AddParagraph(Rp(pnl.Revenue));

            // total bar
            var total = tbl.AddRow();
            total.Shading.Color = Colors.WhiteSmoke;
            total.Cells[0].MergeRight = 4;
            total.Cells[0].AddParagraph("Total Tagihan");
            //total.Cells[5].AddParagraph(Rp(pnl.Revenue)).Format.Font.Bold = true;
        }

        private static void AddOffersTable(Section s, IReadOnlyList<VendorOffer> offers) {
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

            foreach (var o in rows) {
                var r = tbl.AddRow();
                r.Cells[0].AddParagraph(o.Vendor?.VendorName ?? "-");
                r.Cells[1].AddParagraph(o.Round.ToString());
                r.Cells[2].AddParagraph(Rp(o.Price));
                r.Cells[3].AddParagraph("Note");
            }
        }


        private static void AddPnLSummary(Section s, ProfitLoss pnl, Vendor? selectedVendor) {
            var title = s.AddParagraph("Profit & Loss Estimate");
            title.Style = "Heading2";
            title.Format.SpaceAfter = Unit.FromMillimeter(1.5);

            var tbl = s.AddTable();
            tbl.Borders.Width = 0.5;
            tbl.Format.SpaceAfter = Unit.FromMillimeter(4);

            // Kolom kiri label 80mm, kanan nilai 100mm
            tbl.AddColumn(Unit.FromMillimeter(80));
            tbl.AddColumn(Unit.FromMillimeter(100));

            //Row2(tbl, "Revenue (Tagihan PDC)", Rp(pnl.Revenue), shaded: true);
            Row2(tbl, $"Harga Mitra Terpilih{(selectedVendor != null ? $" – {selectedVendor.VendorName}" : "")}", Rp(pnl.SelectedVendorFinalOffer));
            //Row2(tbl, "COST OPERATOR", Rp(pnl.OperatorCost));
            var profit = pnl.Profit;
            Row2(tbl, "PROFIT", Rp(profit), shaded: true, bold: true);

            // Baris % profit terhadap revenue
            var percent = pnl.ProfitPercent;
            Row2(tbl, "% Profit vs Revenue", $"{percent:0.##} %");
        }

        private static void AddSignatures(Section s) {
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
            for (int i = 0; i < 3; i++) {
                rBox.Cells[i].AddParagraph("\n\n\n"); // ruang tanda tangan
            }

            var rCap = tbl.AddRow();
            rCap.Cells[0].AddParagraph("Disetujui oleh\nAnalyst HTE").Format.Alignment = ParagraphAlignment.Center;
            rCap.Cells[1].AddParagraph("Diketahui oleh\nAsisten Manager HTE").Format.Alignment = ParagraphAlignment.Center;
            rCap.Cells[2].AddParagraph("Disahkan oleh\nManager Transport & Logistic").Format.Alignment = ParagraphAlignment.Center;

            s.AddParagraph().Format.SpaceAfter = Unit.FromMillimeter(2);
        }

        private static void AddNotes(Section section, Vendor? selectedVendor, ProfitLoss pnl) {
            var heading = section.AddParagraph("Catatan:");
            heading.Format.Font.Bold = true;
            heading.Format.SpaceBefore = Unit.FromMillimeter(2);
            heading.Format.SpaceAfter = Unit.FromMillimeter(1);

            var baseInfo = new ListInfo {
                ListType = ListType.NumberList1,
                NumberPosition = Unit.FromMillimeter(6)
            };

            AddListItem(section, "Penawaran RFI melibatkan beberapa vendor terdaftar PDC.", baseInfo, first: true);
            AddListItem(section, $"Dari hasil negosiasi, vendor terpilih {selectedVendor?.VendorName ?? "-"}.", baseInfo);
            AddListItem(section, "Actual invoice disesuaikan dengan Surat Jalan.", baseInfo);
            AddListItem(section, "Mitra telah berpengalaman dan memiliki SKT & CSMS untuk pekerjaan ini.", baseInfo);

            // highlight ringkas profit
            var highlight = section.AddParagraph();
            highlight.Format.Shading.Color = Colors.LightYellow;
            highlight.Format.SpaceBefore = Unit.FromMillimeter(1);
            highlight.AddText($"Estimasi Profit: {Rp(pnl.Profit)} ({(pnl.ProfitPercent):0.##}%)");
        }
        // ------------------------ Helpers ------------------------

        private static string Rp(decimal? v) =>
            v.HasValue ? string.Format(Id, "Rp {0:N0}", v.Value) : "-";

        private static void Header(Row r, params string[] captions) {
            for (int i = 0; i < captions.Length; i++) {
                r.Cells[i].AddParagraph(captions[i]);
                r.Cells[i].Shading.Color = Colors.LightGray;
                r.Cells[i].Format.Font.Bold = true;
                r.Cells[i].Format.Alignment = ParagraphAlignment.Center;
            }
            r.TopPadding = 2;
            r.BottomPadding = 2;
        }

        private static void Row2(Table t, string label, string value, bool shaded = false, bool bold = false) {
            var r = t.AddRow();
            r.Cells[0].AddParagraph(label);
            r.Cells[1].AddParagraph(value);
            r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
            if (shaded) {
                r.Cells[0].Shading.Color = Colors.WhiteSmoke;
                r.Cells[1].Shading.Color = Colors.WhiteSmoke;
            }
            if (bold) {
                r.Cells[0].Format.Font.Bold = true;
                r.Cells[1].Format.Font.Bold = true;
            }
        }

        private static void AddListItem(Section section, string text, ListInfo baseInfo, bool first = false) {
            var p = section.AddParagraph(text);
            p.Format.LeftIndent = Unit.FromMillimeter(12);
            p.Format.FirstLineIndent = Unit.FromMillimeter(-6);
            p.Format.SpaceAfter = Unit.FromPoint(1.5);
            p.Format.ListInfo = new ListInfo {
                ListType = baseInfo.ListType,
                NumberPosition = baseInfo.NumberPosition,
                ContinuePreviousList = !first
            };
        }

        private static void DefineStyles(Document doc) {
            var normal = doc.Styles["Normal"];
            normal.Font.Name = "Calibri";
            normal.Font.Size = 9;

            var h1 = doc.Styles.AddStyle("Heading1", "Normal");
            h1.Font.Size = 14;
            h1.Font.Bold = true;
            h1.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(2);

            var h2 = doc.Styles.AddStyle("Heading2", "Normal");
            h2.Font.Size = 10.5;
            h2.Font.Bold = true;
            h2.ParagraphFormat.SpaceBefore = Unit.FromMillimeter(2);
            h2.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(1);
        }
    }
}
