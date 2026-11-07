using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Web.Controllers.Account;
using ProcurementHTE.Web.Models.Admin;

namespace ProcurementHTE.Tests.Controllers;

public sealed class SelfDeactivationTests : IAsyncLifetime
{
    private readonly ServiceProvider _services;
    private readonly UserManager<User> _users;
    private readonly User _admin = NewUser("admin");
    private readonly User _other = NewUser("other");

    public SelfDeactivationTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<AppDbContext>();
        _services = services.BuildServiceProvider();
        _users = _services.GetRequiredService<UserManager<User>>();
    }

    public async Task InitializeAsync()
    {
        await _users.CreateAsync(_admin);
        await _users.CreateAsync(_other);
    }

    public async Task DisposeAsync() => await _services.DisposeAsync();

    [Fact]
    public async Task Power_button_cannot_deactivate_the_signed_in_account()
    {
        var controller = NewController();

        await controller.ToggleActive(_admin.Id);

        Assert.True((await _users.FindByIdAsync(_admin.Id))!.IsActive);
        Assert.NotNull(controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task Power_button_still_deactivates_other_accounts()
    {
        await NewController().ToggleActive(_other.Id);

        Assert.False((await _users.FindByIdAsync(_other.Id))!.IsActive);
    }

    [Fact]
    public async Task Edit_form_cannot_deactivate_the_signed_in_account()
    {
        var controller = NewController();
        var form = new UserFormInputModel
        {
            Id = _admin.Id,
            FirstName = "Admin",
            Email = _admin.Email!,
            IsActive = false,
            SelectedRoles = ["Admin"],
        };

        var result = await controller.Edit(_admin.Id, new UserFormPageViewModel { Form = form });

        Assert.IsType<ViewResult>(result);
        Assert.Equal("Form.IsActive", Assert.Single(controller.ModelState).Key);
        Assert.True((await _users.FindByIdAsync(_admin.Id))!.IsActive);
    }

    [Fact]
    public async Task Edit_form_cannot_remove_admin_from_the_signed_in_account()
    {
        var controller = NewController();
        var form = new UserFormInputModel
        {
            Id = _admin.Id,
            FirstName = "Changed",
            Email = _admin.Email!,
            IsActive = true,
            SelectedRoles = ["Operation"],
        };

        var result = await controller.Edit(_admin.Id, new UserFormPageViewModel { Form = form });

        Assert.IsType<ViewResult>(result);
        Assert.Equal("Form.SelectedRoles", Assert.Single(controller.ModelState).Key);
        Assert.Equal("admin", (await _users.FindByIdAsync(_admin.Id))!.FirstName);
    }

    private static User NewUser(string name) => new()
    {
        UserName = name,
        Email = $"{name}@test",
        FirstName = name,
        LastName = string.Empty,
        IsActive = true,
    };

    private UserManagementController NewController()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, _admin.Id)], "Test")
            ),
        };

        return new UserManagementController(
            _users,
            null!,
            _services.GetRequiredService<RoleManager<Role>>()
        )
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new NoTempDataStore()),
        };
    }

    private sealed class NoTempDataStore : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
