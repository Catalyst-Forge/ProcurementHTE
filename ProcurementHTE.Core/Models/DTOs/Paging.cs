namespace ProcurementHTE.Core.Models.DTOs
{
    public record PagedMeta(int Page, int PageSize, int TotalItems, int TotalPages);

    public record PagedResult<T>(IReadOnlyList<T> Items, int TotalItems);
}
