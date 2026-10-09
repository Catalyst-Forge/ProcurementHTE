namespace ProcurementHTE.Core.Common
{
    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = [];
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int Total { get; init; }
        // An empty result is still one (empty) page, never "page 1 of 0".
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
