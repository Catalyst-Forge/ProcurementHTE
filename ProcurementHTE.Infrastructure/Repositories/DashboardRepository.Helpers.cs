using System.ComponentModel;
using System.Reflection;
using ProcurementHTE.Core.Enums;

namespace ProcurementHTE.Infrastructure.Repositories
{
    public partial class DashboardRepository
    {
        private static string GetStatusDescription(ProcurementStatus status)
        {
            var field = status.GetType().GetField(status.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? status.ToString();
        }
    }
}
