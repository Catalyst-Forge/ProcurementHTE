using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Web.Models.Admin;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class UserManagementController
{
    [HttpGet]
    public async Task<IActionResult> Index(UserFiltersViewModel filters)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(search))
                || (u.Email != null && u.Email.Contains(search))
                || (u.FirstName != null && u.FirstName.Contains(search))
                || (u.LastName != null && u.LastName.Contains(search))
                || (u.JobTitle != null && u.JobTitle.Contains(search))
            );
        }

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            switch (filters.Status)
            {
                case "active":
                    query = query.Where(u => u.IsActive);
                    break;
                case "inactive":
                    query = query.Where(u => !u.IsActive);
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(filters.TwoFactor))
        {
            switch (filters.TwoFactor)
            {
                case "enabled":
                    query = query.Where(u => u.TwoFactorEnabled);
                    break;
                case "disabled":
                    query = query.Where(u => !u.TwoFactorEnabled);
                    break;
            }
        }

        var totalCount = await query.CountAsync();
        var activeCount = await query.CountAsync(u => u.IsActive);
        var inactiveCount = totalCount - activeCount;
        var twoFactorEnabledCount = await query.CountAsync(u => u.TwoFactorEnabled);
        var page = filters.Page <= 0 ? 1 : filters.Page;
        var pageSize = filters.PageSize <= 0 ? 10 : filters.PageSize;
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var resultUsers = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrWhiteSpace(filters.Role) && !roles.Contains(filters.Role))
                continue;

            resultUsers.Add(
                new UserListItemViewModel
                {
                    Id = user.Id,
                    DisplayName = BuildDisplayName(user),
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    JobTitle = user.JobTitle ?? string.Empty,
                    Roles = roles.ToArray(),
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    IsActive = user.IsActive,
                    LastLoginAt = user.LastLoginAt,
                }
            );
        }

        var roleOptions = await _roleManager
            .Roles.OrderBy(r => r.Name)
            .Select(r => new RoleOptionViewModel { Id = r.Id, Name = r.Name! })
            .ToListAsync();

        var viewModel = new UserManagementIndexViewModel
        {
            Filters = filters,
            Users = resultUsers,
            AvailableRoles = roleOptions,
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            TwoFactorEnabledCount = twoFactorEnabledCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        };

        return View(viewModel);
    }
}
