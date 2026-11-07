using ProcurementHTE.Core.Constants;

namespace ProcurementHTE.Web.Authorization
{
    public static class DashboardRoleHelper
    {
        public const string AdminRole = "Admin";

        public const string ManagerTransportRole = "Manager Transport & Logistic";
        public const string AnalystHteRole = "Analyst HTE & LTS";
        public const string OperationRole = "Operation";
        public const string AssistantManagerHteRole = RoleNames.AsstManager;
        public const string VicePresidentRole = "Vice President";
        public const string AppoRole = "AP-PO";
        public const string ArRole = "AR";
        public const string ApInvoiceRole = "AP-Invoice";
        public const string AccrualRole = RoleNames.Accrual;

        public static readonly string[] GeneralRoles =
        [
            ManagerTransportRole,
            AnalystHteRole,
            OperationRole,
            AssistantManagerHteRole,
            VicePresidentRole,
            AppoRole,
            ArRole,
            ApInvoiceRole,
            AccrualRole,
        ];

        public static string GeneralRolesCsv => string.Join(',', GeneralRoles);

        private static readonly Dictionary<string, string> RoleControllerMap = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            [ManagerTransportRole] = "ManagerTransportDashboard",
            [AnalystHteRole] = "AnalystHteDashboard",
            [OperationRole] = "OperationDashboard",
            [AssistantManagerHteRole] = "AssistantManagerHteDashboard",
            [VicePresidentRole] = "VicePresidentDashboard",
            [AppoRole] = "AppoDashboard",
            [ArRole] = "ArDashboard",
            [ApInvoiceRole] = "ApInvoiceDashboard",
            [AccrualRole] = "ArDashboard",
        };

        public static bool IsAdmin(string? role) =>
            !string.IsNullOrWhiteSpace(role)
            && string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase);

        public static bool IsGeneralRole(string? role) =>
            !string.IsNullOrWhiteSpace(role)
            && GeneralRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

        public static string? FindGeneralRole(IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                if (IsGeneralRole(role))
                {
                    return role;
                }
            }

            return null;
        }

        public static string? FindKnownRole(IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                if (IsAdmin(role))
                {
                    return AdminRole;
                }
            }

            return FindGeneralRole(roles) ?? roles.FirstOrDefault();
        }

        public static bool TryGetControllerForRole(string? role, out string controllerName)
        {
            controllerName = string.Empty;
            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            if (IsAdmin(role))
            {
                controllerName = "AdminDashboard";
                return true;
            }

            if (RoleControllerMap.TryGetValue(role, out var controller))
            {
                controllerName = controller;
                return true;
            }

            return false;
        }
    }
}
