using System.Linq.Expressions;

namespace ProcurementHTE.Core.Common;

public sealed record ListSort(string Key, bool Descending);

/// <summary>The sort a list page asked for, if any.</summary>
public interface IListSortProvider
{
    ListSort? Current { get; }
}

public static class ListSortExtensions
{
    /// <summary>
    /// Orders by the requested column when it is one of <paramref name="columns"/>, otherwise
    /// by <paramref name="defaultOrder"/>. The tie breaker keeps paging stable when values repeat.
    /// </summary>
    public static IOrderedQueryable<T> SortBy<T>(
        this IQueryable<T> source,
        ListSort? sort,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> columns,
        Expression<Func<T, object?>> tieBreaker,
        Func<IQueryable<T>, IOrderedQueryable<T>> defaultOrder
    )
    {
        if (sort is null || !columns.TryGetValue(sort.Key, out var column))
            return defaultOrder(source);

        var ordered = sort.Descending ? source.OrderByDescending(column) : source.OrderBy(column);
        return ordered.ThenBy(tieBreaker);
    }
}
