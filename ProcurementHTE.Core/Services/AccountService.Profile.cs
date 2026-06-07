using ProcurementHTE.Core.Enums;
using ProcurementHTE.Core.Models.DTOs;

namespace ProcurementHTE.Core.Services;

public partial class AccountService
{
    public async Task<AccountOverviewDto> GetOverviewAsync(
        string userId,
        CancellationToken ct = default
    )
    {
        var user = await RequireUserAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);
        var roleList = roles?.ToList() ?? new List<string>();

        return new AccountOverviewDto(
            user.Id,
            user.UserName!,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.JobTitle,
            user.PhoneNumber,
            await BuildAvatarUrlAsync(user, ct),
            user.PhoneNumberConfirmed,
            user.EmailConfirmed,
            await _userManager.GetTwoFactorEnabledAsync(user),
            user.TwoFactorMethod,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt,
            user.PasswordChangedAt,
            roleList,
            ParseRecoveryCodes(user.RecoveryCodesJson),
            user.RecoveryCodesHidden,
            user.RecoveryCodesGeneratedAt
        );
    }

    public async Task UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await RequireUserAsync(request.UserId);

        var emailChanged = !string.Equals(
            user.Email,
            request.Email,
            StringComparison.OrdinalIgnoreCase
        );
        var phoneChanged = !string.Equals(
            user.PhoneNumber,
            request.PhoneNumber,
            StringComparison.OrdinalIgnoreCase
        );

        if (!string.Equals(user.UserName, request.UserName, StringComparison.OrdinalIgnoreCase))
        {
            var userNameResult = await _userManager.SetUserNameAsync(user, request.UserName);
            EnsureSucceeded(userNameResult, "Gagal memperbarui username.");
        }

        if (emailChanged)
        {
            var emailResult = await _userManager.SetEmailAsync(user, request.Email);
            EnsureSucceeded(emailResult, "Gagal memperbarui email.");
            user.EmailConfirmed = false;
        }

        user.FirstName = request.FirstName ?? string.Empty;
        user.LastName = request.LastName ?? string.Empty;
        user.JobTitle = request.JobTitle;
        user.PhoneNumber = request.PhoneNumber;
        if (phoneChanged)
            user.PhoneNumberConfirmed = false;

        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        EnsureSucceeded(updateResult, "Gagal memperbarui profil.");

        await LogEventAsync(
            user.Id,
            SecurityLogEventType.ProfileUpdated,
            true,
            "Profil dasar diperbarui.",
            null,
            null,
            ct
        );
    }

    public async Task<string?> UploadAvatarAsync(
        UploadAvatarRequest request,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await RequireUserAsync(request.UserId);

        if (request.Content.CanSeek)
            request.Content.Position = 0;

        var fileName = SanitizeFileName(request.FileName);
        var objectKey = $"avatars/{user.Id}/{Guid.NewGuid():N}-{fileName}";

        if (!string.IsNullOrWhiteSpace(user.AvatarObjectKey))
            await SafeDeleteAsync(user.AvatarObjectKey);

        await _objectStorage.UploadAsync(
            _storageOptions.Bucket,
            objectKey,
            request.Content,
            request.Length,
            request.ContentType,
            ct
        );

        user.AvatarObjectKey = objectKey;
        user.AvatarFileName = fileName;
        user.AvatarUpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        EnsureSucceeded(updateResult, "Gagal menyimpan foto profil.");

        await LogEventAsync(
            user.Id,
            SecurityLogEventType.AvatarUpdated,
            true,
            "Foto profil diperbarui.",
            null,
            null,
            ct
        );

        return await BuildAvatarUrlAsync(user, ct);
    }

    public async Task RemoveAvatarAsync(string userId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId);
        if (string.IsNullOrWhiteSpace(user.AvatarObjectKey))
            return;

        await SafeDeleteAsync(user.AvatarObjectKey);
        user.AvatarObjectKey = null;
        user.AvatarFileName = null;
        user.AvatarUpdatedAt = null;

        var updateResult = await _userManager.UpdateAsync(user);
        EnsureSucceeded(updateResult, "Gagal menghapus foto profil.");

        await LogEventAsync(
            user.Id,
            SecurityLogEventType.AvatarUpdated,
            true,
            "Foto profil dihapus.",
            null,
            null,
            ct
        );
    }
}
