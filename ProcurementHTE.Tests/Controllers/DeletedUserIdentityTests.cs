using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Web.Authorization;
using ProcurementHTE.Web.Controllers.Account;
using ProcurementHTE.Web.Models.Admin;

namespace ProcurementHTE.Tests.Controllers;

public sealed class DeletedUserIdentityTests : IAsyncLifetime
{
    private readonly ServiceProvider _services;
    private readonly UserManager<User> _users;
    private readonly User _admin = NewUser("signedin");
    private readonly User _active = NewUser("active");
    private readonly User _deleted = NewUser("Admin");

    public DeletedUserIdentityTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services
            .AddIdentityCore<User>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<IndonesianIdentityErrorDescriber>();
        _services = services.BuildServiceProvider();
        _users = _services.GetRequiredService<UserManager<User>>();
    }

    public async Task InitializeAsync()
    {
        _deleted.IsDeleted = true;
        await _users.CreateAsync(_admin);
        await _users.CreateAsync(_active);
        await _users.CreateAsync(_deleted);
    }

    public async Task DisposeAsync() => await _services.DisposeAsync();

    [Fact]
    public async Task Create_points_at_the_restore_menu_when_a_deleted_account_holds_the_email()
    {
        var controller = NewController();

        var result = await controller.Create(Page(_deleted.Email!.ToUpperInvariant()));

        Assert.IsType<ViewResult>(result);
        Assert.Contains("Data Terhapus", ErrorOn(controller, "Form.Email"));
    }

    [Fact]
    public async Task Create_reports_an_active_duplicate_once_in_indonesian()
    {
        var controller = NewController();

        await controller.Create(Page(_active.Email!));

        Assert.Contains("sudah dipakai", ErrorOn(controller, string.Empty));
    }

    [Fact]
    public async Task Create_uses_the_email_as_the_username()
    {
        await NewController().Create(Page("baru@test"));

        Assert.Equal("baru@test", (await _users.FindByEmailAsync("baru@test"))!.UserName);
    }

    [Fact]
    public async Task Edit_moves_the_username_to_the_email()
    {
        var page = Page("pindah@test");
        page.Form.Id = _active.Id;

        await NewController().Edit(_active.Id, page);

        var user = await _users.FindByIdAsync(_active.Id);
        Assert.Equal("pindah@test", user!.Email);
        Assert.Equal("pindah@test", user.UserName);
    }

    [Fact]
    public async Task Edit_cannot_take_an_email_held_by_a_deleted_account()
    {
        var controller = NewController();
        var page = Page(_deleted.Email!);
        page.Form.Id = _active.Id;

        var result = await controller.Edit(_active.Id, page);

        Assert.IsType<ViewResult>(result);
        Assert.Contains("Data Terhapus", ErrorOn(controller, "Form.Email"));
        Assert.Equal(_active.Email, (await _users.FindByIdAsync(_active.Id))!.Email);
    }

    private static string ErrorOn(Controller controller, string key) =>
        Assert.Single(controller.ModelState[key]!.Errors).ErrorMessage;

    private static UserFormPageViewModel Page(string email) =>
        new()
        {
            Form = new UserFormInputModel
            {
                FirstName = "Baru",
                Email = email,
                IsActive = true,
            },
        };

    private static User NewUser(string name) => new()
    {
        UserName = $"{name.ToLowerInvariant()}@test",
        Email = $"{name.ToLowerInvariant()}@test",
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
