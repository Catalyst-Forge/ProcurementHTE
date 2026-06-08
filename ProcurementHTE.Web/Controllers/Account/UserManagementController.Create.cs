using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Models.Admin;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class UserManagementController
{
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var roles = await GetRoleOptionsAsync();
        var vm = new UserFormPageViewModel
        {
            Form = new UserFormInputModel(),
            Roles = roles,
            GeneratedPassword = GeneratePassword(),
        };

        return View("Edit", vm);
    }

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
                EmailConfirmed = true,
                LockoutEnabled = true,
            };

            var password = model.GeneratedPassword ?? GeneratePassword();
            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                model.Roles = await GetRoleOptionsAsync();
                model.GeneratedPassword = password;
                return View("Edit", model);
            }

            if (model.Form.SelectedRoles != null && model.Form.SelectedRoles.Any())
            {
                var addRoleResult = await _userManager.AddToRolesAsync(
                    user,
                    model.Form.SelectedRoles
                );
                if (!addRoleResult.Succeeded)
                {
                    foreach (var error in addRoleResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

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
            ModelState.AddModelError(
                string.Empty,
                $"Terjadi kesalahan saat membuat user: {ex.Message}"
            );

            model.Roles = await GetRoleOptionsAsync();
            if (string.IsNullOrWhiteSpace(model.GeneratedPassword))
                model.GeneratedPassword = GeneratePassword();
            return View("Edit", model);
        }
    }
}
