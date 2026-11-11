using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Controllers.Ldp;

namespace ProcurementHTE.Tests.Authorization;

// Hapus terpilih on the LDP and Arsip LDP is for Admin only (Diah, 11 Nov 2026).
public class LdpBulkDeleteAccessTests
{
    public static TheoryData<Type> Controllers => new() { typeof(LdpController), typeof(LdpArchiveController) };

    private static MethodInfo DeleteSelected(Type controller) =>
        controller.GetMethod("DeleteSelected", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

    [Theory]
    [MemberData(nameof(Controllers))]
    public void Only_admin_deletes_the_ticked_rows(Type controller)
    {
        var action = DeleteSelected(controller);

        Assert.NotNull(action);
        Assert.Equal("Admin", action.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
    }

    [Theory]
    [MemberData(nameof(Controllers))]
    public void Deleting_the_ticked_rows_is_a_post_with_the_antiforgery_token(Type controller)
    {
        var action = DeleteSelected(controller);

        Assert.NotNull(action.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}
