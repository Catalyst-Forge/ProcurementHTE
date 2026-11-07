using Microsoft.AspNetCore.Http.Extensions;
using ProcurementHTE.Core.Common;

namespace ProcurementHTE.Web.Extensions;

public static class ListQueryExtensions
{
    private const string SortKey = "sort";
    private const string DirectionKey = "dir";

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

public sealed class QueryListSortProvider(IHttpContextAccessor accessor) : IListSortProvider
{
    public ListSort? Current => accessor.HttpContext?.Request.ReadListSort();
}
