using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Core.Services;
using ProcurementHTE.Infrastructure.Data;

namespace ProcurementHTE.Tests.Services;

public sealed class ProfileEmailTests : IAsyncLifetime
{
    private readonly ServiceProvider _services;
    private readonly UserManager<User> _users;
    private readonly User _user = new()
    {
        UserName = "lama@test",
        Email = "lama@test",
        FirstName = "Lama",
        LastName = string.Empty,
    };

    public ProfileEmailTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services
            .AddIdentityCore<User>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AppDbContext>();
        _services = services.BuildServiceProvider();
        _users = _services.GetRequiredService<UserManager<User>>();
    }

    public async Task InitializeAsync() => await _users.CreateAsync(_user);

    public async Task DisposeAsync() => await _services.DisposeAsync();

    [Fact]
    public async Task Changing_the_email_moves_the_username_with_it()
    {
        await UpdateEmailAsync("baru@test");

        var user = await _users.FindByIdAsync(_user.Id);
        Assert.Equal("baru@test", user!.Email);
        Assert.Equal("baru@test", user.UserName);
    }

    [Fact]
    public async Task Keeping_the_email_leaves_the_username_alone()
    {
        await UpdateEmailAsync(_user.Email!);

        Assert.Equal("lama@test", (await _users.FindByIdAsync(_user.Id))!.UserName);
    }

    private Task UpdateEmailAsync(string email) =>
        new AccountService(
            _users,
            TimeProvider.System,
            Stub<IObjectStorage>.Create(),
            Options.Create(new ObjectStorageOptions { Bucket = "test" }),
            Stub<IUserSessionRepository>.Create(),
            Stub<IUserSecurityLogRepository>.Create(),
            Stub<IEmailSender>.Create(),
            Stub<ISmsSender>.Create(),
            NullLogger<AccountService>.Instance
        ).UpdateProfileAsync(
            new UpdateProfileRequest
            {
                UserId = _user.Id,
                FirstName = "Lama",
                Email = email,
            }
        );

    private class Stub<T> : DispatchProxy
        where T : class
    {
        public static T Create() => DispatchProxy.Create<T, Stub<T>>();

        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method!.ReturnType == typeof(Task) ? Task.CompletedTask : null;
    }
}
