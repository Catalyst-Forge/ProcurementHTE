using System.Reflection;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class LdpRepository
    {
        /// <summary>
        /// Most LDP columns are worked out from related records after mapping, so sorting and
        /// filtering happen here rather than in SQL. Column keys are DTO property names.
        /// </summary>
        public static PropertyInfo? FindColumn(string? key)
        {
            if (key is null)
                return null;

            var property = typeof(LdpRecapDto).GetProperty(
                key,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
            );
            if (property is null)
                return null;

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            return typeof(IComparable).IsAssignableFrom(type) ? property : null;
        }

        private static List<ColumnFilter> KnownFilters(IReadOnlyDictionary<string, string> filters) =>
            filters
                .Select(filter => (Column: FindColumn(filter.Key), filter.Value))
                .Where(filter => filter.Column is not null)
                .Select(filter => new ColumnFilter(filter.Column!, filter.Value))
                .ToList();

        private sealed record ColumnFilter(PropertyInfo Column, string Value)
        {
            // Matches what the table shows, so a search finds what the user can see.
            public bool Matches(LdpRecapDto item) =>
                Column.GetValue(item) switch
                {
                    null => false,
                    DateTime date => Contains(date.ToString("dd MMM yyyy HH:mm")),
                    decimal number => Contains(number.ToString("N0")) || Contains(number.ToString("0")),
                    var value => Contains(value.ToString()),
                };

            private bool Contains(string? text) =>
                text?.Contains(Value, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static IEnumerable<LdpRecapDto> Sort(
            IEnumerable<LdpRecapDto> items,
            PropertyInfo property,
            bool descending
        )
        {
            var comparer = Comparer<object?>.Create(CompareValues);
            return descending
                ? items.OrderByDescending(property.GetValue, comparer)
                : items.OrderBy(property.GetValue, comparer);
        }

        // Empty values sort lowest, as NULL does in the lists sorted by SQL Server.
        private static int CompareValues(object? left, object? right)
        {
            if (left is null || right is null)
                return (left is null ? 0 : 1) - (right is null ? 0 : 1);

            return left is string text
                ? StringComparer.OrdinalIgnoreCase.Compare(text, (string)right)
                : ((IComparable)left).CompareTo(right);
        }
    }
}
