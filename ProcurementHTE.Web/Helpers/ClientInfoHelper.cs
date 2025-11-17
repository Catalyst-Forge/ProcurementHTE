using System;

namespace ProcurementHTE.Web.Helpers
{
    public static class ClientInfoHelper
    {
        public static (string Device, string Browser) Parse(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return ("Tidak diketahui", "Tidak diketahui");

            var browser = userAgent.Contains("Edg", StringComparison.OrdinalIgnoreCase)
                ? "Microsoft Edge"
                : userAgent.Contains("OPR", StringComparison.OrdinalIgnoreCase)
                    ? "Opera"
                    : userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase)
                        ? "Google Chrome"
                        : userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase)
                            ? "Mozilla Firefox"
                            : userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase)
                                ? "Safari"
                                : "Tidak diketahui";

            var device = userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
                ? "Mobile"
                : "Desktop";

            return (device, browser);
        }
    }
}
