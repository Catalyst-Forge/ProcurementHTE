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
                $"Gagal menghapus user. Pastikan user tidak dipakai di data lain. Detail: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
