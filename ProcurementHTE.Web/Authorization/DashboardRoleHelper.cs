using System;
using System.Collections.Generic;
using System.Linq;

namespace ProcurementHTE.Web.Authorization
{
    public static class DashboardRoleHelper
    {
        public const string AdminRole = "Admin";

        public const string ManagerTransportRole = "Manager Transport & Logistic";
        public const string AnalystHteRole = "Analyst HTE & LTS";
        public const string HteRole = "HTE";
        public const string OperationRole = "Operation";
        public const string AssistantManagerHteRole = "Assistant Manager HTE";
        public const string VicePresidentRole = "Vice President";
        public const string HseRole = "HSE";
        public const string SupplyChainManagementRole = "Supply Chain Management";

        public static readonly string[] GeneralRoles =
        [
            ManagerTransportRole,
            AnalystHteRole,
            HteRole,
            OperationRole,
            AssistantManagerHteRole,
            VicePresidentRole,
            HseRole,
            SupplyChainManagementRole,
        ];

        public static string GeneralRolesCsv => string.Join(',', GeneralRoles);

        private static readonly Dictionary<string, string> RoleControllerMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [ManagerTransportRole] = "ManagerTransportDashboard",
                [AnalystHteRole] = "AnalystHteDashboard",
                [HteRole] = "HteDashboard",
                [OperationRole] = "OperationDashboard",
                [AssistantManagerHteRole] = "AssistantManagerHteDashboard",
                [VicePresidentRole] = "VicePresidentDashboard",
                [HseRole] = "HseDashboard",
                [SupplyChainManagementRole] = "SupplyChainManagementDashboard",
            };

        public static bool IsAdmin(string? role) =>
            !string.IsNullOrWhiteSpace(role) &&
            string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase);

        public static bool IsGeneralRole(string? role) =>
            !string.IsNullOrWhiteSpace(role) &&
            GeneralRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

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
