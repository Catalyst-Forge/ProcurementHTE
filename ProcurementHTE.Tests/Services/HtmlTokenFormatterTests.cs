using ProcurementHTE.Core.Services;

namespace ProcurementHTE.Tests.Services;

public class HtmlTokenFormatterTests
{
    [Fact]
    public void ReplaceToken_ReplacesTokenWithOptionalWhitespace()
    {
        var html = "<p>{{ JobName }}</p><span>{{JobName}}</span>";

        var result = HtmlTokenFormatter.ReplaceToken(html, "JobName", "Rig Move");

        Assert.Equal("<p>Rig Move</p><span>Rig Move</span>", result);
    }

    [Fact]
    public void ReplaceToken_UsesDashForNullValue()
    {
        var result = HtmlTokenFormatter.ReplaceToken("{{ VendorName }}", "VendorName", null);

        Assert.Equal("-", result);
    }

    [Fact]
    public void FormatDate_UsesIndonesianLongDate()
    {
        var result = HtmlTokenFormatter.FormatDate(new DateTime(2026, 6, 7));

        Assert.Equal("07 Juni 2026", result);
    }
}
