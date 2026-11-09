using System.Security.Claims;
using ProcurementHTE.Core.Constants;

namespace ProcurementHTE.Web.Authorization;

/// <summary>
/// Who may act on each module. Every signed-in user can open these pages, so
/// a write action and its button must both follow these roles.
/// </summary>
public static class ActionRoles
{
    public const string Procurement = "Admin, Operation, Analyst HTE & LTS";
    public const string Cancel = "Admin, Operation";
    public const string ApPo = "Admin, AP-PO";
    public const string ApInvoice = "Admin, AP-Invoice";
    public const string Ar = "Admin, AR";
    public const string Accrual = $"Admin, AR, {RoleNames.Accrual}";
    public const string Approver =
        $"Admin, Analyst HTE & LTS, {RoleNames.AsstManager}, Manager Transport & Logistic, Vice President";

    public static bool Allows(ClaimsPrincipal user, string roles) =>
        roles.Split(',', StringSplitOptions.TrimEntries).Any(user.IsInRole);

    // Nobody picks a job up any more: AR fills accrual like the Accrual role does.
    public static bool CanFillAccrual(ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole(RoleNames.Accrual) || user.IsInRole("AR");
}
