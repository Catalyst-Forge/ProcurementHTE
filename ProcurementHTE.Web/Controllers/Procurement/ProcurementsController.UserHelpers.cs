using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Controllers.ProcurementModule;

public partial class ProcurementsController
{
    private async Task<IEnumerable<SelectListItem>> BuildUserSelectListAsync(
        string roleName,
        string? selectedUserId
    )
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        return users
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new SelectListItem
            {
                Value = user.Id,
                Text = BuildUserDisplayName(user),
                Selected = string.Equals(user.Id, selectedUserId, StringComparison.Ordinal),
            })
            .ToList();
    }

    private static string BuildUserDisplayName(User user)
    {
        var parts = new[] { user.FirstName, user.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        var fullName = string.Join(' ', parts);

        return string.IsNullOrWhiteSpace(fullName)
            ? user.UserName ?? user.Email ?? "Unknown User"
            : fullName;
    }

    private async Task PopulateUserFullNamesAsync(Procurement procurement)
    {
        var map = await GetUserNamesAsync(
            new[]
            {
                procurement.PicOpsUserId,
                procurement.AnalystHteUserId,
                procurement.AssistantManagerUserId,
                procurement.ManagerUserId,
            }
        );

        string Resolve(string? id) =>
            !string.IsNullOrEmpty(id) && map.TryGetValue(id, out var name) ? name : id ?? "-";

        ViewBag.PicOpsName = Resolve(procurement.PicOpsUserId);
        ViewBag.AnalystName = Resolve(procurement.AnalystHteUserId);
        ViewBag.AssistantManagerName = Resolve(procurement.AssistantManagerUserId);
        ViewBag.ManagerName = Resolve(procurement.ManagerUserId);
    }

    private async Task<Dictionary<string, string>> BuildUserNameMapAsync(
        IEnumerable<Procurement> procurements
    )
    {
        var ids = procurements
            .SelectMany(procurement =>
                new[]
                {
                    procurement.PicOpsUserId,
                    procurement.AnalystHteUserId,
                    procurement.AssistantManagerUserId,
                    procurement.ManagerUserId,
                }
            )
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ids.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await GetUserNamesAsync(ids);
    }

    private async Task<Dictionary<string, string>> GetUserNamesAsync(IEnumerable<string?> ids)
    {
        var uniqueIds = ids.Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        var users = await _userManager
            .Users.Where(user => uniqueIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.UserName,
                user.Email,
            })
            .ToListAsync();

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in users)
        {
            map[user.Id] =
                !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName
                : !string.IsNullOrWhiteSpace(user.UserName) ? user.UserName!
                : user.Email ?? user.Id;
        }

        foreach (var id in uniqueIds)
        {
            if (!map.ContainsKey(id))
                map[id] = id;
        }

        return map;
    }

    private bool CanUserEditProcurementByStatus(Procurement procurement)
    {
        var statusName = procurement.Status?.StatusName ?? "";
        var prePickupStatuses = new[] { "Draft", "Created", "Waiting Pickup" };
        var earlyEditRoles = new[] { "Admin", "Operation", "Assistant Manager HTE" };
        var lateEditRoles = new[] { "AR", "AP-Invoice", "Analyst HTE & LTS", "Supply Chain Management" };

        var isPrePickup = prePickupStatuses.Contains(statusName, StringComparer.OrdinalIgnoreCase);
        var userRoles = User.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();

        if (userRoles.Any(role => earlyEditRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
            return true;

        return !(
            userRoles.Any(role => lateEditRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            && isPrePickup
        );
    }
}
