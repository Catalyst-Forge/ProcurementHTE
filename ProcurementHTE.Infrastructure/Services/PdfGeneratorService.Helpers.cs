using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;

namespace ProcurementHTE.Infrastructure.Services;

public partial class PdfGeneratorService
{
    private static string Rp(decimal? v) =>
        v.HasValue ? string.Format(Id, "Rp {0:N0}", v.Value) : "-";

    private static void Header(Row r, params string[] captions)
    {
        for (int i = 0; i < captions.Length; i++)
        {
            r.Cells[i].AddParagraph(captions[i]);
            r.Cells[i].Shading.Color = Colors.LightGray;
            r.Cells[i].Format.Font.Bold = true;
            r.Cells[i].Format.Alignment = ParagraphAlignment.Center;
        }
        r.TopPadding = 2;
        r.BottomPadding = 2;
    }

    private static void Row2(
        Table t,
        string label,
        string value,
        bool shaded = false,
        bool bold = false
    )
    {
        var r = t.AddRow();
        r.Cells[0].AddParagraph(label);
        r.Cells[1].AddParagraph(value);
        r.Cells[1].Format.Alignment = ParagraphAlignment.Right;
        if (shaded)
        {
            r.Cells[0].Shading.Color = Colors.WhiteSmoke;
            r.Cells[1].Shading.Color = Colors.WhiteSmoke;
        }
        if (bold)
        {
            r.Cells[0].Format.Font.Bold = true;
            r.Cells[1].Format.Font.Bold = true;
        }
    }

    private static void AddListItem(
        Section section,
        string text,
        ListInfo baseInfo,
        bool first = false
    )
    {
        var p = section.AddParagraph(text);
        p.Format.LeftIndent = Unit.FromMillimeter(12);
        p.Format.FirstLineIndent = Unit.FromMillimeter(-6);
        p.Format.SpaceAfter = Unit.FromPoint(1.5);
        p.Format.ListInfo = new ListInfo
        {
            ListType = baseInfo.ListType,
            NumberPosition = baseInfo.NumberPosition,
            ContinuePreviousList = !first,
        };
    }

    private static void DefineStyles(Document doc)
    {
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
