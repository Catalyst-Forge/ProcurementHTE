using System.Text.RegularExpressions;

namespace ProcurementHTE.Tests.Views;

public partial class ConsoleOutputTests
{
    // Failures may still use console.warn and console.error.
    [GeneratedRegex(@"console\.(?:log|info|debug|table|trace|group)\(")]
    private static partial Regex DebugOutput();

    // Its logger only writes on localhost.
    private const string LocalOnlyLogger = "profit-loss.core.js";

    [Fact]
    public void Scripts_write_no_debug_output_to_the_browser_console()
    {
        var web = FindWebRoot();
        var scripts = Directory.EnumerateFiles(Path.Combine(web, "wwwroot", "js"), "*.js", SearchOption.AllDirectories);
        var views = Directory.EnumerateFiles(Path.Combine(web, "Views"), "*.cshtml", SearchOption.AllDirectories);

        var offenders = scripts
            .Concat(views)
            .Where(file => Path.GetFileName(file) != LocalOnlyLogger)
            .Where(file => DebugOutput().IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(web, file));

        Assert.Empty(offenders);
    }

    private static string FindWebRoot()
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
