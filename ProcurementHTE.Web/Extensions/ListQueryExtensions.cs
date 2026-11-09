using Microsoft.AspNetCore.Http.Extensions;
using ProcurementHTE.Core.Common;

namespace ProcurementHTE.Web.Extensions;

public static class ListQueryExtensions
{
    private const string SortKey = "sort";
    private const string DirectionKey = "dir";
    private const string FilterPrefix = "f.";

    public static ListSort? ReadListSort(this HttpRequest request)
    {
        var key = request.Query[SortKey].ToString();
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var descending = string.Equals(
            request.Query[DirectionKey].ToString(),
            "desc",
            StringComparison.OrdinalIgnoreCase
        );
        return new ListSort(key, descending);
    }

    /// <summary>Per-column filters, sent as f.Column=value.</summary>
    public static IReadOnlyDictionary<string, string> ReadListFilters(this HttpRequest request) =>
        request
            .Query.Where(item =>
                item.Key.StartsWith(FilterPrefix, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(item.Value)
            )
            .ToDictionary(
                item => item.Key[FilterPrefix.Length..],
                item => item.Value.ToString().Trim(),
                StringComparer.OrdinalIgnoreCase
            );

    /// <summary>The month and year picked above the list, sent as bulan=1..12 and tahun=yyyy.</summary>
    public static ListPeriod ReadListPeriod(this HttpRequest request) =>
        ListPeriod.Read(request.Query[PeriodMonthKey].ToString(), request.Query[PeriodYearKey].ToString());

    public const string PeriodMonthKey = "bulan";
    public const string PeriodYearKey = "tahun";

    public static string WithoutListFilters(this HttpRequest request) =>
        request.WithQuery(
            request
                .Query.Keys.Where(key => key.StartsWith(FilterPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(key => (key, (string?)null))
                .Append(("page", null))
                .ToArray()
        );

    /// <summary>The current query string with these keys replaced, or removed when null.</summary>
    public static string WithQuery(this HttpRequest request, params (string Key, string? Value)[] changes)
    {
        var builder = new QueryBuilder();
        foreach (var (key, values) in request.Query)
        {
            if (changes.Any(change => string.Equals(change.Key, key, StringComparison.OrdinalIgnoreCase)))
                continue;

            foreach (var value in values)
                builder.Add(key, value ?? string.Empty);
        }

        foreach (var (key, value) in changes)
        {
            if (value is not null)
                builder.Add(key, value);
        }

        return builder.ToQueryString().HasValue ? builder.ToQueryString().Value! : "?";
    }

    public static string WithPage(this HttpRequest request, int page) =>
        request.WithQuery(("page", page.ToString()));

    public static string WithSort(this HttpRequest request, string key, bool descending) =>
        request.WithQuery(
            (SortKey, key),
            (DirectionKey, descending ? "desc" : "asc"),
            ("page", "1"),
            ("partial", null)
        );
}

public sealed class QueryListStateProvider(IHttpContextAccessor accessor)
    : IListSortProvider,
        IListFilterProvider
{
    public ListSort? Current => accessor.HttpContext?.Request.ReadListSort();

    public IReadOnlyDictionary<string, string> Filters =>
        accessor.HttpContext?.Request.ReadListFilters() ?? new Dictionary<string, string>();

    public ListPeriod Period => accessor.HttpContext?.Request.ReadListPeriod() ?? ListPeriod.None;
}
