using System.Text.RegularExpressions;

namespace ProcurementHTE.Tests.Views;

public partial class PartialReferenceTests
{
    [GeneratedRegex(@"(?:Html\.(?:Partial|PartialAsync|RenderPartialAsync)\(\s*""|<partial\s+name="")([^""]+)""")]
    private static partial Regex PartialReference();

    [Fact]
    public void Every_partial_name_resolves_the_way_MVC_looks_it_up()
    {
        var viewsRoot = FindViewsRoot();

        var unresolved = Directory
            .EnumerateFiles(viewsRoot, "*.cshtml", SearchOption.AllDirectories)
            .SelectMany(view =>
            {
                var folder = Path.GetRelativePath(viewsRoot, view).Split(Path.DirectorySeparatorChar)[0];
                return PartialReference()
                    .Matches(File.ReadAllText(view))
                    .Select(match => match.Groups[1].Value)
                    .Where(name => !Resolves(viewsRoot, folder, view, name))
                    .Select(name => $"{Path.GetRelativePath(viewsRoot, view)} -> {name}");
            })
            .ToList();

        Assert.Empty(unresolved);
    }

    // A bare name is searched in the controller's view folder and Shared, not next to the caller.
    private static bool Resolves(string viewsRoot, string folder, string view, string name)
    {
        if (name.StartsWith('~') || name.StartsWith('/'))
        {
            return File.Exists(Path.Combine(Path.GetDirectoryName(viewsRoot)!, name.TrimStart('~', '/')));
        }

        if (name.EndsWith(".cshtml", StringComparison.Ordinal))
        {
            return File.Exists(Path.Combine(Path.GetDirectoryName(view)!, name));
        }

        return File.Exists(Path.Combine(viewsRoot, folder, $"{name}.cshtml"))
            || File.Exists(Path.Combine(viewsRoot, "Shared", $"{name}.cshtml"));
    }

    private static string FindViewsRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "ProcurementHTE.Web", "Views");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("ProcurementHTE.Web/Views not found.");
    }
}
