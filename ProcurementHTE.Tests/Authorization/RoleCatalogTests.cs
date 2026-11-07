using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Web.Authorization;

namespace ProcurementHTE.Tests.Authorization;

public sealed class RoleCatalogTests : IAsyncLifetime
{
    private static readonly string[] ClientRoles =
    [
        "Accrual",
        "Admin",
        "Analyst HTE & LTS",
        "AP-Invoice",
        "AP-PO",
        "AR",
        "Asst. Manager Transport & Logistic",
        "Manager Transport & Logistic",
        "Operation",
        "Vice President",
    ];

    private readonly ServiceProvider _services;
    private readonly RoleManager<Role> _roles;

    public RoleCatalogTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<AppDbContext>();
        _services = services.BuildServiceProvider();
        _roles = _services.GetRequiredService<RoleManager<Role>>();
    }

    public Task InitializeAsync() => RoleSeeder.SeedAsync(_roles);

    public async Task DisposeAsync() => await _services.DisposeAsync();

    [Fact]
    public async Task Seeder_creates_exactly_the_client_role_list()
    {
        var seeded = await _roles.Roles.Select(r => r.Name!).ToListAsync();

        Assert.Equal(ClientRoles.Order(), seeded.Order());
    }

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public void Every_role_but_admin_opens_a_dashboard(string role)
    {
        Assert.True(DashboardRoleHelper.IsGeneralRole(role));
        Assert.True(DashboardRoleHelper.TryGetControllerForRole(role, out _));
    }

    [Fact]
    public void Every_dashboard_role_is_on_the_client_list()
    {
        Assert.Empty(DashboardRoleHelper.GeneralRoles.Except(ClientRoles));
    }

    // A role name that matches nothing denies access without any error, and a rename can
    // miss one that sits inside a longer string.
    [Fact]
    public void Every_authorize_attribute_names_a_role_on_the_client_list()
    {
        var controllers = typeof(ActionRoles)
            .Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type));

        var named = controllers
            .SelectMany(type =>
                type.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                    .Concat(type.GetMethods().SelectMany(m => m.GetCustomAttributes<AuthorizeAttribute>(inherit: true)))
            )
            .Select(attribute => attribute.Roles)
            .OfType<string>()
            .SelectMany(SplitRoles);

        Assert.Empty(named.Distinct().Except(ClientRoles));
    }

    [Fact]
    public void Every_action_role_list_names_a_role_on_the_client_list()
    {
        var named = typeof(ActionRoles)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)
            .SelectMany(SplitRoles);

        Assert.Empty(named.Distinct().Except(ClientRoles));
    }

    public static TheoryData<string> NonAdminRoles() => new(ClientRoles.Where(role => role != "Admin"));

    private static string[] SplitRoles(string roles) =>
        roles.Split(',', StringSplitOptions.TrimEntries);
}
