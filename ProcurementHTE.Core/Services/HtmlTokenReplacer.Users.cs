using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private async Task<ProcurementUserNames> ResolveProcurementUserNamesAsync(
            Procurement proc
        )
        {
            return new ProcurementUserNames(
                await ResolveProcurementUserNameAsync(proc.PicOpsUserId),
                await ResolveProcurementUserNameAsync(proc.AnalystHteUserId),
                await ResolveProcurementUserNameAsync(proc.AssistantManagerUserId),
                await ResolveProcurementUserNameAsync(proc.ManagerUserId)
            );
        }

        private async Task<string> ResolveProcurementUserNameAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return "-";

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return userId;

            if (!string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName;

            if (!string.IsNullOrWhiteSpace(user.UserName))
                return user.UserName;

            return user.Email ?? userId;
        }

        private async Task<string> ResolveFirstUserNameByRoleAsync(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName) || roleName == "-")
                return "-";

            var users = await _userManager.GetUsersInRoleAsync(roleName);
            var user = users?.FirstOrDefault();

            if (user == null)
                return "-";

            if (!string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName;

            if (!string.IsNullOrWhiteSpace(user.UserName))
                return user.UserName;

            return user.Email ?? "-";
        }

        private async Task<string> ResolveUserNameByIdAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return "-";

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return "-";

            if (!string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName;

            if (!string.IsNullOrWhiteSpace(user.UserName))
                return user.UserName;

            return user.Email ?? "-";
        }

        private async Task<string> ResolveUserNameByRoleWithProcDataAsync(
            string? roleName,
            string? analystHteName,
            string? asstMgrName,
            string? mgrName
        )
        {
            if (string.IsNullOrWhiteSpace(roleName) || roleName == "-")
                return "-";

            if (roleName.Equals("Analyst HTE & LTS", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(analystHteName)
                    ? analystHteName
                    : await ResolveFirstUserNameByRoleAsync(roleName);

            if (roleName.Equals("Assistant Manager HTE", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(asstMgrName)
                    ? asstMgrName
                    : await ResolveFirstUserNameByRoleAsync(roleName);

            if (roleName.Equals("Manager Transport & Logistic", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(mgrName)
                    ? mgrName
                    : await ResolveFirstUserNameByRoleAsync(roleName);

            return await ResolveFirstUserNameByRoleAsync(roleName);
        }
    }
}
