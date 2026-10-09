using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Navigation;

namespace ProcurementHTE.Tests.Navigation;

/// <summary>
/// Hiding a menu does not lock its page. Every role-limited menu must point at
/// a controller whose [Authorize(Roles = ...)] admits exactly the same roles.
/// </summary>
public class SidebarAuthorizationTests
{
    // Guarded another way: permission policies per action (Procurements) or a
    // role check inside the action (ProcurementTracking.PendingApprovals).
    private static readonly HashSet<string> GuardedElsewhere = ["Procurements", "ProcurementTracking"];

    private static readonly Type[] Controllers = typeof(SidebarMenu)
        .Assembly.GetTypes()
        .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
        .ToArray();

    public static TheoryData<string> RoleLimitedControllers()
    {
        var data = new TheoryData<string>();
        foreach (var name in SidebarMenu.Sections
            .SelectMany(s => s.Items)
            .Where(i => i.Roles != null && !GuardedElsewhere.Contains(i.Controller))
            .Select(i => i.Controller)
            .Distinct())
        {
            data.Add(name);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(RoleLimitedControllers))]
    public void Controller_AuthorizesSameRolesAsItsMenu(string controllerName)
    {
        var item = SidebarMenu.Sections.SelectMany(s => s.Items).First(i => i.Controller == controllerName);
        var controller = Controllers.Single(t => t.Name == $"{controllerName}Controller");

        var authorizedRoles = controller
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet();

        Assert.True(
            authorizedRoles.SetEquals(item.Roles!),
            $"{controller.Name} allows [{string.Join(", ", authorizedRoles)}] but its menu shows to [{string.Join(", ", item.Roles!)}]."
        );
    }
}
