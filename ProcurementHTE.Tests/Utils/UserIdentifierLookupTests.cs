using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Infrastructure.Data;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Tests.Utils;

public class UserIdentifierLookupTests : IAsyncLifetime
{
    private ServiceProvider _services = null!;
    private UserManager<User> _userManager = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services
            .AddIdentityCore<User>(o => o.User.RequireUniqueEmail = true)
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AppDbContext>();

        _services = services.BuildServiceProvider();
        _userManager = _services.GetRequiredService<UserManager<User>>();

        await CreateAsync("budi.santoso", "budi@pdc.test", nip: "19870512");
        await CreateAsync("sari", "sari@pdc.test", nip: null);
    }

    public async Task DisposeAsync() => await _services.DisposeAsync();

    private async Task CreateAsync(string userName, string email, string? nip)
    {
        var result = await _userManager.CreateAsync(
            new User
            {
                UserName = userName,
                Email = email,
                FirstName = userName,
                Nip = nip,
            },
            "Rahasia#123"
        );
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    [Theory]
    [InlineData("19870512")]
    [InlineData("  19870512 ")]
    [InlineData("budi@pdc.test")]
    [InlineData("BUDI@PDC.TEST")]
    [InlineData("budi.santoso")]
    public async Task FindAsync_ResolvesNipEmailAndUsernameToSameUser(string identifier)
    {
        var user = await UserIdentifierLookup.FindAsync(_userManager, identifier);

        Assert.NotNull(user);
        Assert.Equal("budi.santoso", user.UserName);
    }

    [Fact]
    public async Task FindAsync_UserWithoutNip_CanStillLogInWithEmail()
    {
        var user = await UserIdentifierLookup.FindAsync(_userManager, "sari@pdc.test");

        Assert.NotNull(user);
        Assert.Null(user.Nip);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("99999999")]
    [InlineData("tidak@ada.test")]
    public async Task FindAsync_ReturnsNull_ForBlankOrUnknownIdentifier(string? identifier)
    {
        Assert.Null(await UserIdentifierLookup.FindAsync(_userManager, identifier));
    }

    [Fact]
    public void Model_NipHasUniqueFilteredIndex_SoUsersWithoutNipDoNotCollide()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var index = context
            .Model.FindEntityType(typeof(User))!
            .GetIndexes()
            .Single(i => i.Properties.Any(p => p.Name == nameof(User.Nip)));

        Assert.True(index.IsUnique);
        Assert.Equal("[Nip] IS NOT NULL", index.GetFilter());
    }
}
