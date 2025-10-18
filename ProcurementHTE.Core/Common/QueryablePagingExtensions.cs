using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ProcurementHTE.Core.Common
{
    public static class QueryablePagingExtensions
    {
        // Generic paging (entity/DTO apa pun)
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> source,
            int page,
            int pageSize,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            CancellationToken ct = default
        )
        {
            if (page < 1)
                page = 1;
            if (pageSize < 1)
                pageSize = 25;

            var total = await source.CountAsync(ct);

            var ordered = orderBy != null ? orderBy(source) : source;
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total,
            };
        }
    }
}
