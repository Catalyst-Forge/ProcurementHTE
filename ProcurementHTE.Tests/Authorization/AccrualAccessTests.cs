using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ProcurementHTE.Web.Authorization;
using ProcurementHTE.Web.Controllers.ArPickup;
using ProcurementHTE.Web.Controllers.Dashboard;

namespace ProcurementHTE.Tests.Authorization;

public class AccrualAccessTests
{
    private static ClaimsPrincipal UserWithRole(string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test"));

    [Theory]
    [InlineData("Admin", false, true)]
    [InlineData("Accrual", false, true)]
    [InlineData("AR", true, true)]
    [InlineData("AR", false, false)]
    [InlineData("AP-PO", true, false)]
    public void CanFillAccrual_only_holds_ar_to_the_pickup(string role, bool pickedUp, bool allowed)
    {
        Assert.Equal(allowed, ActionRoles.CanFillAccrual(UserWithRole(role), pickedUp));
    }

    [Fact]
    public void Save_accepts_every_role_that_can_fill_accrual()
    {
        var attribute = typeof(AccrualController)
            .GetMethod(nameof(AccrualController.Save))!
            .GetCustomAttribute<AuthorizeAttribute>()!;

        Assert.Equal(ActionRoles.Accrual, attribute.Roles);
        Assert.True(ActionRoles.Allows(UserWithRole("Accrual"), attribute.Roles!));
        Assert.True(ActionRoles.Allows(UserWithRole("AR"), attribute.Roles!));
        Assert.False(ActionRoles.Allows(UserWithRole("AP-PO"), attribute.Roles!));
    }

    [Fact]
    public void Accrual_role_can_open_the_dashboard_it_is_sent_to()
    {
        Assert.True(DashboardRoleHelper.TryGetControllerForRole("Accrual", out var controller));
        Assert.Equal("ArDashboard", controller);

        var attribute = typeof(ArDashboardController).GetCustomAttribute<AuthorizeAttribute>(inherit: false)!;
        Assert.True(ActionRoles.Allows(UserWithRole("Accrual"), attribute.Roles!));
    }
}
