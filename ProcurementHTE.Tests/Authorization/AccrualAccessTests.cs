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
    [InlineData("Admin", true)]
    [InlineData("Accrual", true)]
    [InlineData("AR", true)]
    [InlineData("AP-PO", false)]
    [InlineData("AP-Invoice", false)]
    public void CanFillAccrual_needs_only_the_role_now_that_pickup_is_gone(string role, bool allowed)
    {
        Assert.Equal(allowed, ActionRoles.CanFillAccrual(UserWithRole(role)));
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
