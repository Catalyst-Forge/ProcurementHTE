using System.Globalization;
using System.Linq.Expressions;

namespace ProcurementHTE.Core.Common;

/// <summary>
/// The month and year picked above a job list, compared with each job's start date. Either may be
/// empty: a year alone is that year, a month and a year is that month, and a month alone is that
/// month in every year. Both empty means no filter.
/// </summary>
public sealed record ListPeriod(int? Month, int? Year)
{
    public static readonly ListPeriod None = new(null, null);

    private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");

    public bool IsEmpty => Month is null && Year is null;

    /// <summary>The month names the picker offers, January first.</summary>
    public static IReadOnlyList<string> MonthNames { get; } =
        Enumerable.Range(1, 12).Select(month => Indonesian.DateTimeFormat.GetMonthName(month)).ToArray();

    /// <summary>The period in the query string. A value out of range is left out, as if empty.</summary>
    public static ListPeriod Read(string? month, string? year)
    {
        int? m = int.TryParse(month, NumberStyles.None, CultureInfo.InvariantCulture, out var mm) && mm is >= 1 and <= 12 ? mm : null;
        int? y = int.TryParse(year, NumberStyles.None, CultureInfo.InvariantCulture, out var yy) && yy is > 1900 and < 3000 ? yy : null;
        return new ListPeriod(m, y);
    }

    public bool Contains(DateTime? date) =>
        IsEmpty
        || date is { } d && (Year is null || d.Year == Year) && (Month is null || d.Month == Month);

    /// <summary>The days the period covers as [From, To), when it has a year.</summary>
    public (DateTime From, DateTime To)? Range =>
        Year is not { } year ? null
        : Month is { } month ? (new DateTime(year, month, 1), new DateTime(year, month, 1).AddMonths(1))
        : (new DateTime(year, 1, 1), new DateTime(year + 1, 1, 1));

    public IQueryable<T> Apply<T>(IQueryable<T> source, Expression<Func<T, DateTime>> date) =>
        Apply(source, date.Body, date.Parameters[0]);

    public IQueryable<T> Apply<T>(IQueryable<T> source, Expression<Func<T, DateTime?>> date) =>
        Apply(source, date.Body, date.Parameters[0]);

    // A year becomes a date range, so SQL Server can use an index; a month alone needs DATEPART.
    // Values go in as captured fields, so EF sends them as parameters.
    private IQueryable<T> Apply<T>(IQueryable<T> source, Expression body, ParameterExpression parameter)
    {
        if (IsEmpty)
            return source;

        var type = body.Type;
        Expression test;
        if (Range is var (from, to))
        {
            test = Expression.AndAlso(
                Expression.GreaterThanOrEqual(body, Value(from, type)),
                Expression.LessThan(body, Value(to, type))
            );
        }
        else
        {
            var day = type == typeof(DateTime) ? body : Expression.Property(body, nameof(Nullable<DateTime>.Value));
            var month = Expression.Property(day, nameof(DateTime.Month));
            test = Expression.Equal(month, Value(Month!.Value, typeof(int)));
            if (type != typeof(DateTime))
                test = Expression.AndAlso(Expression.NotEqual(body, Expression.Constant(null, type)), test);
        }

        return source.Where(Expression.Lambda<Func<T, bool>>(test, parameter));
    }

    private static Expression Value<TValue>(TValue value, Type type) =>
        Expression.Convert(Expression.Field(Expression.Constant(new Box<TValue>(value)), nameof(Box<TValue>.Value)), type);

    private sealed class Box<TValue>(TValue value)
    {
        public readonly TValue Value = value;
    }
}
