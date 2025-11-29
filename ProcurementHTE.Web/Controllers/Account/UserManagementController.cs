using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.Admin;

namespace ProcurementHTE.Web.Controllers.Account
{
    // Hanya role Admin yang boleh akses
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<Role> _roleManager;

        public UserManagementController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<Role> roleManager
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // LIST + FILTER
        [HttpGet]
        public async Task<IActionResult> Index(UserFiltersViewModel filters)
        {
            var query = _userManager.Users.AsQueryable();

            // Search: Nama, Username, Email, JobTitle
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

            // Filter status (aktif / nonaktif)
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

            // Filter 2FA
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

            // Statistik
            var totalCount = await query.CountAsync();
            var activeCount = await query.CountAsync(u => u.IsActive);
            var inactiveCount = totalCount - activeCount;
            var twoFactorEnabledCount = await query.CountAsync(u => u.TwoFactorEnabled);

            // Paging
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

                // Filter role (kalau dipilih)
                if (!string.IsNullOrWhiteSpace(filters.Role) && !roles.Contains(filters.Role))
                {
                    continue;
                }

                var displayName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = user.UserName ?? user.Email ?? user.Id;
                }

                resultUsers.Add(
                    new UserListItemViewModel
                    {
                        Id = user.Id,
                        DisplayName = displayName,
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

        // GET: Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var roles = await GetRoleOptionsAsync();

            var vm = new UserFormPageViewModel
            {
                Form = new UserFormInputModel(),
                Roles = roles,
                // Password auto-generate, nanti ditampilkan ke admin
                GeneratedPassword = GeneratePassword(),
            };

            // Pakai view Edit yang sama (Create & Edit satu view)
            return View("Edit", vm);
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormPageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = await GetRoleOptionsAsync();
                if (string.IsNullOrWhiteSpace(model.GeneratedPassword))
                    model.GeneratedPassword = GeneratePassword();
                return View("Edit", model);
            }

            try
            {
                var user = new User
                {
                    UserName = model.Form.UserName,
                    Email = model.Form.Email,
                    FirstName = model.Form.FirstName,
                    LastName = model.Form.LastName ?? string.Empty,
                    JobTitle = model.Form.JobTitle,
                    PhoneNumber = model.Form.PhoneNumber,
                    IsActive = model.Form.IsActive,
                    EmailConfirmed = true, // bisa disesuaikan kebutuhan
                    LockoutEnabled = true,
                };

                var password = model.GeneratedPassword ?? GeneratePassword();

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    model.Roles = await GetRoleOptionsAsync();
                    model.GeneratedPassword = password;
                    return View("Edit", model);
                }

                // Set Roles
                if (model.Form.SelectedRoles != null && model.Form.SelectedRoles.Any())
                {
                    var addRoleResult = await _userManager.AddToRolesAsync(
                        user,
                        model.Form.SelectedRoles
                    );
                    if (!addRoleResult.Succeeded)
                    {
                        foreach (var error in addRoleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        model.Roles = await GetRoleOptionsAsync();
                        model.GeneratedPassword = password;
                        return View("Edit", model);
                    }
                }

                TempData["SuccessMessage"] =
                    $"User {user.UserName} berhasil dibuat. Password awal: {password}";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(ex.Message, $"{ex}. Terjadi kesalahan saat membuat user.");

                model.Roles = await GetRoleOptionsAsync();
                if (string.IsNullOrWhiteSpace(model.GeneratedPassword))
                    model.GeneratedPassword = GeneratePassword();
                return View("Edit", model);
            }
        }

        // GET: Edit
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await GetRoleOptionsAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var vm = new UserFormPageViewModel
            {
                Form = new UserFormInputModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    JobTitle = user.JobTitle,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    SelectedRoles = userRoles.ToList(),
                },
                Roles = roles,
            };

            return View(vm);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserFormPageViewModel model)
        {
            if (id != model.Form.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                model.Roles = await GetRoleOptionsAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            try
            {
                var wasActive = user.IsActive;

                user.FirstName = model.Form.FirstName;
                string? LastName = model.Form.LastName;
                user.Email = model.Form.Email;
                user.UserName = model.Form.UserName;
                user.JobTitle = model.Form.JobTitle;
                user.PhoneNumber = model.Form.PhoneNumber;
                user.IsActive = model.Form.IsActive;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    model.Roles = await GetRoleOptionsAsync();
                    return View(model);
                }

                // Update Roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                var selectedRoles = model.Form.SelectedRoles ?? new List<string>();

                var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
                var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

                var rolesChanged = false;

                if (rolesToAdd.Any())
                {
                    var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                    if (!addResult.Succeeded)
                    {
                        foreach (var error in addResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        model.Roles = await GetRoleOptionsAsync();
                        return View(model);
                    }
                }

                if (rolesToRemove.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    if (!removeResult.Succeeded)
                    {
                        foreach (var error in removeResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        model.Roles = await GetRoleOptionsAsync();
                        return View(model);
                    }
                }

                rolesChanged = rolesToAdd.Any() || rolesToRemove.Any();
                if (rolesChanged || wasActive != user.IsActive)
                {
                    await RefreshUserSessionStateAsync(user, user.IsActive);
                }

                var editingSelf = string.Equals(
                    _userManager.GetUserId(User),
                    user.Id,
                    StringComparison.OrdinalIgnoreCase
                );
                if (editingSelf)
                {
                    var stillAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                    if (!stillAdmin)
                    {
                        return RedirectToForbidden();
                    }
                }

                TempData["SuccessMessage"] = $"User {user.UserName} berhasil diperbarui.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    ex.Message,
                    $"{ex}. Terjadi kesalahan saat mengubah user."
                );

                model.Roles = await GetRoleOptionsAsync();
                return View(model);
            }
        }

        // Toggle aktif/nonaktif cepat dari list
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            await RefreshUserSessionStateAsync(user, user.IsActive);

            TempData["SuccessMessage"] =
                $"Status user {user.UserName} diubah menjadi {(user.IsActive ? "Aktif" : "Tidak Aktif")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (
                !string.IsNullOrEmpty(currentUserId)
                && string.Equals(currentUserId, id, StringComparison.OrdinalIgnoreCase)
            )
            {
                TempData["ErrorMessage"] = "Tidak dapat menghapus akun yang sedang digunakan.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Gagal menghapus user: {errors}";
                }
                else
                {
                    TempData["SuccessMessage"] = $"User {user.UserName} berhasil dihapus.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    $"{ex}. Gagal menghapus user. Pastikan user tidak dipakai di data lain.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helpers
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

        // Password random sederhana (boleh kamu ganti dengan aturan yang kamu mau)
        private string GeneratePassword()
        {
            // Contoh: 10 karakter random + "!"
            var randomPart = Guid.NewGuid().ToString("N")[..6];
            return $"Aa1!{randomPart}";
        }
    }
}
