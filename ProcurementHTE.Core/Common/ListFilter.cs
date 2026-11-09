using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace ProcurementHTE.Core.Common;

/// <summary>
/// The value search behind every column header (f.key=value in the URL), applied to the same
/// columns a list sorts on. What a search means follows the column's type:
/// text contains the value; a date falls on the day, month, or year typed ("09 Nov 2026",
/// "Nov 2026", "11/2026", "2026"); a number equals the amount typed ("13.000.000");
/// a yes/no column reads Aktif/Ya or Nonaktif/Tidak/-; an enum matches its name or description.
/// A value the column cannot read finds nothing. Keys the list does not know are ignored.
/// </summary>
public static class ListFilterExtensions
{
    private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    private static readonly string[] DayFormats =
    [
        "d MMM yyyy", "d MMMM yyyy", "d/M/yyyy", "d-M-yyyy", "yyyy-M-d", "d.M.yyyy",
    ];

    private static readonly string[] MonthFormats = ["MMM yyyy", "MMMM yyyy", "M/yyyy", "M-yyyy", "yyyy-M"];

    public static IQueryable<T> FilterBy<T>(
        this IQueryable<T> source,
        IReadOnlyDictionary<string, string>? filters,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> columns
    )
    {
        if (filters is null)
            return source;

        foreach (var (key, raw) in filters)
        {
            var value = raw?.Trim();
            if (string.IsNullOrEmpty(value) || !columns.TryGetValue(key, out var column))
                continue;

            source = source.Where(Matches(column, value));
        }

        return source;
    }

    /// <summary>The row test for one column and one searched value.</summary>
    public static Expression<Func<T, bool>> Matches<T>(Expression<Func<T, object?>> column, string value)
    {
        var body = column.Body is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : column.Body;
        var type = body.Type;
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        Expression test = underlying switch
        {
            _ when underlying == typeof(string) => TextContains(body, value),
            _ when underlying == typeof(DateTime) => DateWithin(body, type, value),
            _ when underlying == typeof(bool) => YesNo(body, type, value),
            _ when underlying.IsEnum => EnumIn(body, type, underlying, value),
            _ when IsNumber(underlying) => NumberEquals(body, type, underlying, value),
            _ => Expression.Constant(false),
        };

        return Expression.Lambda<Func<T, bool>>(test, column.Parameters);
    }

    // Values go in as captured fields, not literals, so EF sends them as SQL parameters.
    private static Expression Parameter<TValue>(TValue value, Type type) =>
        Expression.Convert(Expression.Field(Expression.Constant(new Box<TValue>(value)), nameof(Box<TValue>.Value)), type);

    private sealed class Box<TValue>(TValue value)
    {
        public readonly TValue Value = value;
    }

    private static Expression TextContains(Expression body, string value) =>
        Expression.AndAlso(
            Expression.NotEqual(body, Expression.Constant(null, typeof(string))),
            Expression.Call(body, typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!, Parameter(value, typeof(string)))
        );

    private static Expression DateWithin(Expression body, Type type, string value)
    {
        if (ReadDateRange(value) is not var (from, to))
            return Expression.Constant(false);

        return Expression.AndAlso(
            Expression.GreaterThanOrEqual(body, Parameter(from, type)),
            Expression.LessThan(body, Parameter(to, type))
        );
    }

    /// <summary>The day, month, or year a typed date covers, as [from, to).</summary>
    public static (DateTime From, DateTime To)? ReadDateRange(string value)
    {
        var text = value.Trim();
        if (text.Length == 4 && int.TryParse(text, out var year) && year is > 1900 and < 3000)
            return (new DateTime(year, 1, 1), new DateTime(year + 1, 1, 1));

        foreach (var culture in new[] { Indonesian, English })
        {
            if (DateTime.TryParseExact(text, DayFormats, culture, DateTimeStyles.AllowWhiteSpaces, out var day))
                return (day.Date, day.Date.AddDays(1));
            if (DateTime.TryParseExact(text, MonthFormats, culture, DateTimeStyles.AllowWhiteSpaces, out var month))
                return (new DateTime(month.Year, month.Month, 1), new DateTime(month.Year, month.Month, 1).AddMonths(1));
        }

        return null;
    }

    private static Expression YesNo(Expression body, Type type, string value)
    {
        var wanted = ReadYesNo(value);
        return wanted is null ? Expression.Constant(false) : Expression.Equal(body, Parameter(wanted.Value, type));
    }

    /// <summary>Aktif, Ya, Yes: true. Nonaktif, Tidak, No, "-": false. Anything else: unknown.</summary>
    public static bool? ReadYesNo(string value)
    {
        var text = value.Trim().ToLowerInvariant();
        if (text.Length == 0)
            return null;
        if (text == "-" || text.StartsWith("non") || text.StartsWith("tidak") || text.StartsWith("no") || text is "false" or "0")
            return false;
        if ("aktif".StartsWith(text) || "ya".StartsWith(text) || "yes".StartsWith(text) || text is "true" or "1")
            return true;
        return null;
    }

    private static Expression EnumIn(Expression body, Type type, Type enumType, string value)
    {
        var matches = Enum.GetValues(enumType)
            .Cast<object>()
            .Where(item => EnumText(enumType, item).Any(text => text.Contains(value, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (matches.Count == 0)
            return Expression.Constant(false);

        var list = Array.CreateInstance(type, matches.Count);
        for (var i = 0; i < matches.Count; i++)
            list.SetValue(matches[i], i);

        return Expression.Call(
            typeof(Enumerable), nameof(Enumerable.Contains), [type], Expression.Constant(list), body);
    }

    private static IEnumerable<string> EnumText(Type enumType, object item)
    {
        var name = item.ToString()!;
        yield return name;
        var description = enumType.GetField(name)?.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (description != null)
            yield return description;
    }

    private static bool IsNumber(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(decimal) || type == typeof(double) || type == typeof(short);

    private static Expression NumberEquals(Expression body, Type type, Type numberType, string value)
    {
        if (ReadNumber(value) is not { } amount)
            return Expression.Constant(false);

        object typed;
        try
        {
            typed = Convert.ChangeType(amount, numberType, CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            return Expression.Constant(false);
        }

        if (numberType != typeof(decimal) && Convert.ToDecimal(typed, CultureInfo.InvariantCulture) != amount)
            return Expression.Constant(false);

        var box = Activator.CreateInstance(typeof(Box<>).MakeGenericType(numberType), typed)!;
        var field = Expression.Field(Expression.Constant(box), "Value");
        return Expression.Equal(body, Expression.Convert(field, type));
    }

    /// <summary>An amount typed the Indonesian way: "13.000.000", "Rp 1.500,5", or "13000000".</summary>
    public static decimal? ReadNumber(string value)
    {
        var text = value.Trim();
        if (text.StartsWith("rp", StringComparison.OrdinalIgnoreCase))
            text = text[2..].TrimStart('.', ' ');
        text = text.Replace(" ", string.Empty);
        return decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, Indonesian, out var amount)
            ? amount
            : null;
    }
}
