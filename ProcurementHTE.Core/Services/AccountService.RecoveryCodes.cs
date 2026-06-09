using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Core.Services;

public partial class AccountService
{
    public async Task<IEnumerable<string>> GenerateRecoveryCodesAsync(
        string userId,
        CancellationToken ct = default
    )
    {
        var user = await RequireUserAsync(userId);
        var generated = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        var codes = generated?.ToArray() ?? Array.Empty<string>();
        user.RecoveryCodesJson = string.Join(';', codes);
        user.RecoveryCodesHidden = false;
        user.RecoveryCodesGeneratedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _userManager.UpdateAsync(user);

        await LogEventAsync(
            user.Id,
            SecurityLogEventType.TwoFactorMethodChanged,
            true,
            "Recovery codes digenerate ulang.",
            null,
            null,
            ct
        );

        return codes;
    }

    public async Task<IReadOnlyList<string>?> GetRecoveryCodesSnapshotAsync(
        string userId,
        CancellationToken ct = default
    )
    {
        var user = await RequireUserAsync(userId);
        return ParseRecoveryCodes(user.RecoveryCodesJson);
    }

    public async Task SetRecoveryCodesHiddenAsync(
        string userId,
        bool hidden,
        CancellationToken ct = default
    )
    {
        var user = await RequireUserAsync(userId);
        user.RecoveryCodesHidden = hidden;
        await _userManager.UpdateAsync(user);
    }
}
