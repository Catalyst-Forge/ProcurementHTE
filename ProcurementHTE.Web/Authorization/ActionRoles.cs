using System.Security.Claims;

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

    public static bool Allows(ClaimsPrincipal user, string roles) =>
        roles.Split(',', StringSplitOptions.TrimEntries).Any(user.IsInRole);
}
