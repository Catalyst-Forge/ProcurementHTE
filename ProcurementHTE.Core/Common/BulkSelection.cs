namespace ProcurementHTE.Core.Common;

/// <summary>
/// The rows an Admin ticked on a list to act on together. A list page holds at most
/// <see cref="Max"/> rows, so a request naming more did not come from the page.
/// </summary>
public static class BulkSelection
{
    public const int Max = 100;

    /// <summary>
    /// The distinct, non-blank ids in the order they were ticked, or null when there are more
    /// than <see cref="Max"/>.
    /// </summary>
    public static IReadOnlyList<string>? Ids(IEnumerable<string?>? ids)
    {
        var distinct = (ids ?? [])
            .Select(id => id?.Trim())
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return distinct.Count > Max ? null : distinct;
    }
}
