using System.Reflection;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class LdpRepository
    {
        /// <summary>
        /// Most LDP columns are worked out from related records after mapping, so a sorted
        /// report is ordered here rather than in SQL. The sort key is the DTO property name.
        /// </summary>
        public static PropertyInfo? FindSortProperty(ListSort? sort)
        {
            if (sort is null)
                return null;

            var property = typeof(LdpRecapDto).GetProperty(
                sort.Key,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
            );
            if (property is null)
                return null;

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            return typeof(IComparable).IsAssignableFrom(type) ? property : null;
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
