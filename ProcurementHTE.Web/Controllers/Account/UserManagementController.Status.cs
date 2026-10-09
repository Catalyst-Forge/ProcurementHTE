using Microsoft.AspNetCore.Mvc;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class UserManagementController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || user.IsDeleted)
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
        if (user == null || user.IsDeleted)
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

        // Soft delete: the account stops working at once but stays in the database,
        // so procurement and approval history keep its name and an admin can restore it.
        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = currentUserId;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            TempData["ErrorMessage"] = $"Gagal menghapus user: {errors}";
            return RedirectToAction(nameof(Index));
        }

        await RefreshUserSessionStateAsync(user, false);
        TempData["SuccessMessage"] =
            $"User {user.UserName} dihapus dan tidak bisa login lagi. Admin bisa memulihkannya dari menu Data Terhapus.";

        return RedirectToAction(nameof(Index));
    }
}
