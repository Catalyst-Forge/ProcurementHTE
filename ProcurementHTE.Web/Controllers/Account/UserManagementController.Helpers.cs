using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.Admin;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class UserManagementController
{
    private async Task<IReadOnlyList<RoleOptionViewModel>> GetRoleOptionsAsync()
    {
        var roles = await _roleManager
            .Roles.OrderBy(r => r.Name)
            .Select(r => new RoleOptionViewModel { Id = r.Id, Name = r.Name! })
            .ToListAsync();

        return roles;
    }

    private IActionResult RedirectToForbidden()
    {
        var redirectUrl =
            Url.Action("Status", "Error", new { statusCode = StatusCodes.Status403Forbidden })
            ?? "/Error/403";

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers["HX-Redirect"] = redirectUrl;
            return new EmptyResult();
        }

        return Redirect(redirectUrl);
    }

    private async Task RefreshUserSessionStateAsync(User targetUser, bool stillActive)
    {
        if (!stillActive)
        {
            await _userManager.UpdateSecurityStampAsync(targetUser);
            return;
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!string.IsNullOrEmpty(currentUserId) && currentUserId == targetUser.Id)
        {
            await _signInManager.RefreshSignInAsync(targetUser);
        }
        else
        {
            await _userManager.UpdateSecurityStampAsync(targetUser);
        }
    }

    private static string BuildDisplayName(User user)
    {
        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName)
            ? user.UserName ?? user.Email ?? user.Id
            : displayName;
    }

    private string GeneratePassword()
    {
        var randomPart = Guid.NewGuid().ToString("N")[..6];
        return $"Aa1!{randomPart}";
    }
}
