using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Controllers.Ldp;

namespace ProcurementHTE.Tests.Authorization;

public class LdpArchiveRangeAccessTests
{
    private static MethodInfo[] RangeActions() =>
        typeof(LdpArchiveController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == nameof(LdpArchiveController.Range))
            .ToArray();

    [Fact]
    public void Only_admin_opens_or_saves_the_ldp_range()
    {
        var actions = RangeActions();

        Assert.Equal(2, actions.Length);
        Assert.All(actions, action => Assert.Equal("Admin", action.GetCustomAttribute<AuthorizeAttribute>()?.Roles));
    }

    [Fact]
    public void Saving_the_ldp_range_needs_the_antiforgery_token()
    {
        var save = RangeActions().Single(action => action.GetCustomAttribute<HttpPostAttribute>() is not null);

        Assert.NotNull(save.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void Everyone_signed_in_still_reads_arsip_ldp_and_its_range()
    {
        var index = typeof(LdpArchiveController).GetMethod(nameof(LdpArchiveController.Index))!;

        Assert.Null(index.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.NotNull(typeof(LdpArchiveController).GetCustomAttribute<AuthorizeAttribute>());
    }
}
