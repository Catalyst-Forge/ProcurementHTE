using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    private async Task<User?> FindUserAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var normalized = identifier.Trim();
        User? user = null;

        if (normalized.Contains('@'))
            user = await _userManager.FindByEmailAsync(normalized);

        user ??= await _userManager.FindByNameAsync(normalized);
        return user;
    }

    private async Task<User?> FindUserByPhoneAsync(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return null;

        var normalized = IndonesianPhoneNumberFormatter.NormalizeForStorageOrEmpty(rawPhone);
        var digits = new string(normalized.Where(char.IsDigit).ToArray());
        var candidates = new[] { normalized, $"+{digits}", "0" + digits, digits };

        return await _userManager.Users.FirstOrDefaultAsync(u =>
            candidates.Contains(u.PhoneNumber ?? string.Empty)
        );
    }
}
