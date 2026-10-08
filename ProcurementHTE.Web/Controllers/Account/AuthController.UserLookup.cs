using Microsoft.EntityFrameworkCore;
using ProcurementHTE.Core.Models;
using ProcurementHTE.Web.Utils;

namespace ProcurementHTE.Web.Controllers.Account;

public partial class AuthController
{
    private Task<User?> FindUserAsync(string identifier) =>
        UserIdentifierLookup.FindAsync(_userManager, identifier);

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
