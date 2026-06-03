using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Models.ViewModels;

public class AccrualIndexViewModel
{
    public IReadOnlyList<Procurement> Procurements { get; set; } = [];
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public string? Filter { get; set; } = "all";

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
