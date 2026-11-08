namespace ProcurementHTE.Web.Navigation;

/// <summary>
/// The page numbers a pager shows. Long lists keep the first page, the last page, and the
/// pages around the current one, with null marking each gap.
/// </summary>
public static class PageWindow
{
    /// <summary>Seven slots plus two arrows fit a desktop pager row.</summary>
    public const int Wide = 7;

    /// <summary>Five slots plus two 44px arrows fit a 390px phone.</summary>
    public const int Narrow = 5;

    public static IReadOnlyList<int?> For(int page, int totalPages, int slots = Wide)
    {
        if (totalPages < 1)
            return [];

        slots = Math.Max(Narrow, slots | 1);
        page = Math.Clamp(page, 1, totalPages);

        if (totalPages <= slots)
            return Numbers(1, totalPages);

        // Near either end, fill that side instead of drawing a gap that hides a single page.
        // Both ends then keep the same width as the middle while paging.
        var siblings = (slots - 5) / 2;
        var edge = slots - 2;
        if (page <= siblings + 3)
            return [.. Numbers(1, edge), null, totalPages];

        if (page >= totalPages - siblings - 2)
            return [1, null, .. Numbers(totalPages - edge + 1, totalPages)];

        return [1, null, .. Numbers(page - siblings, page + siblings), null, totalPages];
    }

    private static List<int?> Numbers(int from, int to) =>
        Enumerable.Range(from, to - from + 1).Select(number => (int?)number).ToList();
}

/// <summary>What the shared page links need: where we are and how to link to another page.</summary>
/// <remarks>
/// Set <see cref="HxGet"/> and <see cref="HxTarget"/> for a list that swaps itself with htmx; the
/// plain href stays so a link still opens in a new tab.
/// </remarks>
public sealed record PageLinks(int Page, int TotalPages, Func<int, string> Href, string Label = "Navigasi halaman")
{
    public Func<int, string>? HxGet { get; init; }
    public string? HxTarget { get; init; }
    public string? HxIndicator { get; init; }
}
