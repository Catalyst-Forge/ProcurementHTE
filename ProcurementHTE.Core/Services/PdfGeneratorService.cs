using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using MigraDocCore.Rendering;
using PdfSharpCore.Pdf;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services
{
    public class PdfGeneratorService : IPdfGenerator
    {
        public Task<byte[]> GenerateProfitLossPdfAsync(
            ProfitLoss pnl,
            WorkOrder wo,
            Vendor? selectedVendor,
            IReadOnlyList<VendorOffer> offers
        )
        {
            var doc = new Document();
            doc.Info.Title = "Profit & Loss";
            doc.Info.Subject = $"WO {wo.WoNum} - Profit & Loss";
            doc.Info.Author = "Procurement HTE";

            var section = doc.AddSection();
            section.PageSetup.TopMargin = Unit.FromCentimeter(2);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(2);

            // Header
            var h = section.AddParagraph($"Profit & Loss - WO {wo.WoNum}");
            h.Format.Font.Size = 14;
            h.Format.Font.Bold = true;
            h.Format.SpaceAfter = Unit.FromCentimeter(0.5);

            // Ringkasan
            var p = section.AddParagraph();
            p.Format.SpaceAfter = Unit.FromCentimeter(0.3);
            p.AddText($"Tanggal: {pnl.CreatedAt:dd MMM yyyy}\n");
            p.AddText($"Tarif Awal: Rp {pnl.TarifAwal:N0}\n");
            p.AddText($"Tarif Add: Rp {pnl.TarifAdd:N0}\n");
            p.AddText($"KM/25: {pnl.KmPer25}\n");
            p.AddText($"Operator Cost: Rp {pnl.OperatorCost:N0}\n");
            p.AddText($"Revenue: Rp {pnl.Revenue:N0}\n");
            if (selectedVendor is not null)
                p.AddText(
                    $"Best Vendor: {selectedVendor.VendorName} (Rp {pnl.SelectedVendorFinalOffer:N0})\n"
                );
            p.AddText($"Profit: Rp {pnl.Profit:N0} ({pnl.ProfitPercent:N2}%)");

            // Tabel penawaran per vendor
            var grouped = offers
                .GroupBy(x => x.Vendor?.VendorName ?? x.VendorId)
                .OrderBy(g => g.Key)
                .ToList();

            if (grouped.Count > 0)
            {
                var table = section.AddTable();
                table.Borders.Width = 0.5;
                table.AddColumn(Unit.FromCentimeter(6)); // Vendor
                table.AddColumn(Unit.FromCentimeter(3)); // Round
                table.AddColumn(Unit.FromCentimeter(6)); // Price

                var header = table.AddRow();
                header.Shading.Color = Colors.LightGray;
                header.Cells[0].AddParagraph("Vendor");
                header.Cells[1].AddParagraph("Round");
                header.Cells[2].AddParagraph("Harga");

                foreach (var g in grouped)
                {
                    foreach (var item in g.OrderBy(x => x.Round))
                    {
                        var row = table.AddRow();
                        row.Cells[0].AddParagraph(g.Key);
                        row.Cells[1].AddParagraph(item.Round.ToString());
                        row.Cells[2].AddParagraph($"Rp {item.Price:N0}");
                    }
                }
            }

            // Render PDF ke byte[]
            var renderer = new PdfDocumentRenderer(true) { Document = doc };
            renderer.RenderDocument();
            using var ms = new MemoryStream();
            renderer.PdfDocument.Save(ms);
            return Task.FromResult(ms.ToArray());
        }
    }
}
