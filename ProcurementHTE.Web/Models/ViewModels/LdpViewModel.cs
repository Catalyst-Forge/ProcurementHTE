using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Web.Models.ViewModels
{
    public class LdpIndexViewModel
    {
        public IReadOnlyList<LdpRecapDto> Items { get; set; } = [];

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 25;

        public int Total { get; set; }

        public string? Search { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

        public bool HasPrev => Page > 1;

        public bool HasNext => Page < TotalPages;
    }
}
