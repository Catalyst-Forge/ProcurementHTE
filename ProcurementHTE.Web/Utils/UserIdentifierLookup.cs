using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Utils;

/// <summary>
/// Resolves what a user typed into a login or recovery form: an email when it
/// contains '@', otherwise a NIP.
/// </summary>
public static class UserIdentifierLookup
{
    public static async Task<User?> FindAsync(UserManager<User> userManager, string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var normalized = identifier.Trim();
        var user = normalized.Contains('@')
            ? await userManager.FindByEmailAsync(normalized)
            : await userManager.Users.FirstOrDefaultAsync(u => u.Nip == normalized);

        return user is { IsDeleted: false } ? user : null;
    }
}
