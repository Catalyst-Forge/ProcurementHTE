using System.Linq.Expressions;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Infrastructure.Repositories;

/// <summary>
/// The columns each list may sort on, keyed by the sort-key on its table headers. Anything
/// else in the URL is ignored.
/// </summary>
public static class ListSortColumns
{
    public static readonly IReadOnlyDictionary<string, Expression<Func<Vendor, object?>>> Vendors =
        Columns<Vendor>(("vendor", v => v.VendorName), ("location", v => v.City));

    public static readonly IReadOnlyDictionary<string, Expression<Func<DocumentType, object?>>> DocumentTypes =
        Columns<DocumentType>(("name", d => d.Name), ("description", d => d.Description));

    public static readonly IReadOnlyDictionary<string, Expression<Func<JobTypes, object?>>> JobTypes =
        Columns<JobTypes>(("name", t => t.TypeName), ("description", t => t.Description));

    public static readonly IReadOnlyDictionary<string, Expression<Func<PurchaseRequisition, object?>>> PurchaseRequisitions =
        Columns<PurchaseRequisition>(
            ("prNumber", pr => pr.PrNumber),
            ("requestDate", pr => pr.RequestDate),
            ("description", pr => pr.Description),
            ("document", pr => pr.DocumentFileName),
            ("procurements", pr => pr.Procurements.Count),
            ("createdBy", pr => pr.CreatedByUser!.FullName),
            ("createdAt", pr => pr.CreatedAt)
        );

    public static readonly IReadOnlyDictionary<string, Expression<Func<Procurement, object?>>> Procurements =
        Columns<Procurement>(
            ("procNum", p => p.ProcNum),
            ("jobName", p => p.JobName),
            ("wonum", p => p.Wonum),
            ("spmp", p => p.SpmpNumber),
            ("startDate", p => p.StartDate),
            ("jobType", p => p.JobType!.TypeName),
            ("region", p => p.ProjectRegion),
            ("status", p => p.Status!.StatusName),
            ("raNumber", p => p.RaNumber),
            ("analyst", p => p.AnalystHteUser!.FullName),
            ("asstManager", p => p.AssistantManagerUser!.FullName),
            ("manager", p => p.ManagerUser!.FullName),
            ("pickedBy", p => p.AppoUser!.FullName),
            ("invoice", p => p.SANo ?? p.SP3No),
            ("accrualStatus", p => p.StatusAccrual),
            ("noAccrual", p => p.NoAccrual),
            ("potensiAccrual", p => p.PotensiAccrual),
            ("filledBy", p => p.AccrualFilledByUser!.FullName),
            ("revenue", p => p.ProfitLosses.OrderByDescending(pl => pl.CreatedAt).Select(pl => pl.AccrualAmount).FirstOrDefault()),
            ("cost", p => p.ProfitLosses.OrderByDescending(pl => pl.CreatedAt).Select(pl => (decimal?)pl.SelectedVendorFinalOffer).FirstOrDefault()),
            ("profitPercent", p => p.ProfitLosses.OrderByDescending(pl => pl.CreatedAt).Select(pl => (decimal?)pl.ProfitPercent).FirstOrDefault()),
            ("vendor", p => p.ProfitLosses.OrderByDescending(pl => pl.CreatedAt).Select(pl => pl.SelectedVendor.VendorName).FirstOrDefault())
        );

    public static readonly IReadOnlyDictionary<string, Expression<Func<Procurement, object?>>> PendingApprovals =
        Columns<Procurement>(
            ("procNum", p => p.ProcNum),
            ("wonum", p => p.Wonum),
            ("jobName", p => p.JobName),
            ("status", p => p.ProcurementStatus),
            ("documentDate", p => p.DocumentDate),
            ("sentAt", p => p.ApprovalTokenGeneratedAt)
        );

    public static readonly IReadOnlyDictionary<string, Expression<Func<User, object?>>> Users =
        Columns<User>(
            ("name", u => u.FullName),
            ("email", u => u.Email),
            ("jobTitle", u => u.JobTitle),
            ("status", u => u.IsActive),
            ("twoFactor", u => u.TwoFactorEnabled),
            ("lastLogin", u => u.LastLoginAt)
        );

    private static Dictionary<string, Expression<Func<T, object?>>> Columns<T>(
        params (string Key, Expression<Func<T, object?>> Column)[] columns
    ) => columns.ToDictionary(c => c.Key, c => c.Column, StringComparer.OrdinalIgnoreCase);
}
