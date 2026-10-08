using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Web.Utils;

/// <summary>
/// Resolves what a user typed into a login or recovery form: an email when it
/// contains '@', otherwise a NIP, and finally a username for legacy accounts.
/// </summary>
public static class UserIdentifierLookup
{
    public static async Task<User?> FindAsync(UserManager<User> userManager, string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var normalized = identifier.Trim();
        User? user;

        if (normalized.Contains('@'))
            user = await userManager.FindByEmailAsync(normalized);
        else
            user = await userManager.Users.FirstOrDefaultAsync(u => u.Nip == normalized);

        user ??= await userManager.FindByNameAsync(normalized);
        return user;
    }
}
