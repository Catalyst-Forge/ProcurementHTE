using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Common;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Web.Extensions;

namespace ProcurementHTE.Web.ViewComponents;

/// <summary>
/// The month and year picker above a job list. It sits inside the list's search form, so a search
/// keeps the period; Pending Approvals has no search form, so there it brings its own
/// (<paramref name="standalone"/>). The years offered are those with jobs in the list's source.
/// </summary>
public sealed class PeriodFilterViewComponent(IListPeriodYears years) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string source = "procurements", bool standalone = false)
    {
        var ct = HttpContext.RequestAborted;
        var offered = source switch
        {
            "archive" => await years.ArchiveYearsAsync(ct),
            "ldp" => (await years.ProcurementYearsAsync(ct)).Union(await years.ArchiveYearsAsync(ct)).ToList(),
            _ => await years.ProcurementYearsAsync(ct),
        };

        var current = Request.ReadListPeriod();
        var list = offered.ToHashSet();
        if (current.Year is { } picked)
            list.Add(picked);

        return View(new PeriodFilterViewModel(current, list.OrderByDescending(year => year).ToList(), standalone));
    }
}

public sealed record PeriodFilterViewModel(ListPeriod Current, IReadOnlyList<int> Years, bool Standalone);
