using System.Text.RegularExpressions;

namespace ProcurementHTE.Tests.Views;

public partial class AccessibilityConventionTests
{
    // A <button> whose only content is a Bootstrap icon, e.g. <button ...><i class="bi bi-trash"></i></button>.
    [GeneratedRegex(@"<button\b(?<attrs>[^>]*)>\s*<i\b[^>]*\bclass=""[^""]*\bbi\b[^""]*""[^>]*>\s*</i>\s*</button>", RegexOptions.Singleline)]
    private static partial Regex IconOnlyButton();

    // Not data-title: that one only feeds a confirm dialog.
    [GeneratedRegex(@"(?<![\w-])(aria-label|aria-labelledby|title)=")]
    private static partial Regex AccessibleName();

    [GeneratedRegex(@"<html\b[^>]*\blang=""(?<lang>[^""]+)""")]
    private static partial Regex HtmlLang();

    [Fact]
    public void Every_layout_declares_Indonesian()
    {
        // The app layout said "en", so screen readers read every Indonesian label with English rules.
        var layouts = Directory.EnumerateFiles(Path.Combine(WebRoot(), "Views", "Shared"), "_*Layout.cshtml");

        foreach (var layout in layouts)
        {
            var match = HtmlLang().Match(File.ReadAllText(layout));
            if (match.Success)
                Assert.True(match.Groups["lang"].Value == "id", $"{Path.GetFileName(layout)} declares lang=\"{match.Groups["lang"].Value}\"");
        }
    }

    [Fact]
    public void Icon_only_buttons_have_an_accessible_name()
    {
        // Delete buttons in Job Type Documents and Document Approvals were announced only as "button".
        var web = WebRoot();
        var offenders = Directory
            .EnumerateFiles(Path.Combine(web, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .SelectMany(file => IconOnlyButton()
                .Matches(File.ReadAllText(file))
                .Where(match => !AccessibleName().IsMatch(match.Groups["attrs"].Value))
                .Select(_ => Path.GetRelativePath(web, file)))
            .Distinct()
            .ToList();

        Assert.Empty(offenders);
    }

    private static string WebRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "ProcurementHTE.Web");
            if (Directory.Exists(Path.Combine(candidate, "wwwroot")))
                return candidate;
        }

        throw new DirectoryNotFoundException("ProcurementHTE.Web not found above the test output.");
    }
}
