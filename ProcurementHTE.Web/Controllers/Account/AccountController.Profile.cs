using Microsoft.AspNetCore.Mvc;
using ProcurementHTE.Core.Models.DTOs;
using ProcurementHTE.Web.Models.Account;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AccountController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        [Bind(Prefix = "Profile")] UpdateProfileInputModel model
    )
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Periksa kembali data profil yang Anda masukkan.";
            return RedirectToAction(nameof(Settings));
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        var normalizedEmail = model.Email?.Trim() ?? string.Empty;
        var normalizedPhone = IndonesianPhoneNumberFormatter.NormalizeForStorage(model.PhoneNumber);
        var currentPhone = string.IsNullOrWhiteSpace(user.PhoneNumber)
            ? null
            : user.PhoneNumber.Trim();
        var emailChanged = !string.Equals(
            user.Email,
            normalizedEmail,
            StringComparison.OrdinalIgnoreCase
        );
        var phoneChanged = !string.Equals(
            currentPhone,
            normalizedPhone,
            StringComparison.OrdinalIgnoreCase
        );
        var requiresPhoneVerification =
            phoneChanged
            && !string.IsNullOrWhiteSpace(normalizedPhone)
            && IsSmsVerificationAvailable()
            && !_securityBypassOptions.BypassPhoneVerification;

        var request = new UpdateProfileRequest
        {
            UserId = user.Id,
            FirstName = model.FirstName,
            LastName = model.LastName ?? string.Empty,
            JobTitle = model.JobTitle,
            Email = normalizedEmail,
            UserName = model.UserName,
            PhoneNumber = normalizedPhone,
        };

        try
        {
            await _accountService.UpdateProfileAsync(request, HttpContext.RequestAborted);

            if (emailChanged || requiresPhoneVerification)
            {
                TempData["SuccessMessage"] =
                    "Profil berhasil diperbarui. Silakan verifikasi kontak yang baru diperbarui.";
                var returnUrl = Url.Action(nameof(Settings), "Account") ?? "/";
                return RedirectToAction("ContactVerification", "Auth", new { returnUrl });
            }

            TempData["SuccessMessage"] = "Profil berhasil diperbarui.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal memperbarui profil: {ex.Message}";
        }

        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        [Bind(Prefix = "ChangePassword")] ChangePasswordInputModel model
    )
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(
                "<br/>",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            );
            return RedirectToAction(nameof(Settings));
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        try
        {
            await _accountService.ChangePasswordAsync(
                user.Id,
                model.CurrentPassword,
                model.NewPassword,
                HttpContext.RequestAborted
            );
            TempData["SuccessMessage"] = "Password berhasil diperbarui.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Gagal memperbarui password: {ex.Message}";
        }

        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAvatar(UpdateAvatarInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Pilih gambar valid sebelum menyimpan.";
            return RedirectToAction(nameof(Settings));
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        try
        {
            var (buffer, contentType, fileName) = ParseImagePayload(model.ImageData);
            await using var stream = new MemoryStream(buffer, writable: false);
            var request = new UploadAvatarRequest
            {
                UserId = user.Id,
                Content = stream,
                FileName = fileName,
                ContentType = contentType,
                Length = stream.Length,
            };

            await _accountService.UploadAvatarAsync(request, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Foto profil diperbarui.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] =
                $"Gagal mengunggah foto profil: {ex.Message}. Pastikan format gambar valid.";
        }

        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAvatar()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return RedirectToAction("Login", "Auth");

        await _accountService.RemoveAvatarAsync(user.Id, HttpContext.RequestAborted);
        TempData["SuccessMessage"] = "Foto profil dihapus.";
        return RedirectToAction(nameof(Settings));
    }
}
