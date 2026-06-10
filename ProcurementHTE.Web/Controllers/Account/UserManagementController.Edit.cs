using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Web.Models.Admin;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class UserManagementController
{
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
                    ModelState.AddModelError(string.Empty, error.Description);

                model.Roles = await GetRoleOptionsAsync();
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Form.SelectedRoles ?? new List<string>();
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

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
                        ModelState.AddModelError(string.Empty, error.Description);

                    model.Roles = await GetRoleOptionsAsync();
                    return View(model);
                }
            }

            var rolesChanged = rolesToAdd.Any() || rolesToRemove.Any();
            if (rolesChanged || wasActive != user.IsActive)
                await RefreshUserSessionStateAsync(user, user.IsActive);

            var editingSelf = string.Equals(
                _userManager.GetUserId(User),
                user.Id,
                StringComparison.OrdinalIgnoreCase
            );
            if (editingSelf)
            {
                var stillAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!stillAdmin)
                    return RedirectToForbidden();
            }

            TempData["SuccessMessage"] = $"User {user.UserName} berhasil diperbarui.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                $"Terjadi kesalahan saat mengubah user: {ex.Message}"
            );

            model.Roles = await GetRoleOptionsAsync();
            return View(model);
        }
    }
}
