using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models.ViewModels;

namespace ProcurementHTE.Web.Controllers.Dashboard;

public abstract partial class DashboardBaseController
{
    protected async Task<List<RoleDistributionViewModel>> GetRoleDistributionAsync()
    {
        var roles = new List<(string Name, string Color)>
        {
            ("Admin", "#dc3545"),
            ("Operation", "#0d6efd"),
            ("Analyst HTE & LTS", "#198754"),
            ("Assistant Manager HTE", "#ffc107"),
            ("Manager Transport & Logistic", "#0dcaf0"),
            ("Vice President", "#6f42c1"),
            ("AP-PO", "#fd7e14"),
            ("AR", "#20c997"),
            ("AP-Invoice", "#e83e8c"),
            ("HSE", "#6c757d"),
            ("Supply Chain Management", "#343a40")
        };

        var result = new List<RoleDistributionViewModel>();
        foreach (var role in roles)
        {
            var usersInRole = await UserManager.GetUsersInRoleAsync(role.Name);
            if (usersInRole.Count > 0)
            {
                result.Add(new RoleDistributionViewModel
                {
                    RoleName = role.Name,
                    UserCount = usersInRole.Count,
                    Color = role.Color
                });
            }
        }
        return result.OrderByDescending(r => r.UserCount).ToList();
    }

    protected async Task<IActionResult> RenderDashboardAsync(
        string viewPath,
        string roleName,
        CancellationToken ct = default
    )
    {
        var user = await UserManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = await BuildDashboardAsync(user, roleName, ct);
        return View(viewPath, model);
    }
}
