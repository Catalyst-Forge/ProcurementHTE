namespace ProcurementHTE.Web.Models.ViewModels;

public static class DashboardDisplayHelper
{
    public static string StatusBadgeClass(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "created" => "success",
            "draft" => "secondary",
            "in progress" => "primary",
            "completed" => "info",
            _ => "danger"
        };

    public static string WinRateClass(decimal winRate) =>
        winRate switch
        {
            >= 70 => "success",
            >= 40 => "info",
            >= 20 => "warning",
            _ => "danger"
        };
}
