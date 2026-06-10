using System.Globalization;
using System.Text.RegularExpressions;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public static class HtmlTokenFormatter
    {
        public static readonly CultureInfo Id = new("id-ID");

        public static string FormatDate(DateTime? value)
        {
            return value?.ToString("dd MMMM yyyy", Id) ?? "-";
        }

        public static string FormatDecimal(decimal? value, string format = "N0")
        {
            return value.HasValue ? value.Value.ToString(format, Id) : "-";
        }

        public static string FormatCurrency(decimal? value, string format = "C0")
        {
            return value.HasValue ? value.Value.ToString(format, Id) : "-";
        }

        public static string GetUserName(User? user)
        {
            return user?.FullName ?? user?.UserName ?? user?.Email ?? "-";
        }

        public static string ReplaceToken(string html, string tokenName, string? value)
        {
            var replacement = value ?? "-";
            var pattern = $"{{{{\\s*{Regex.Escape(tokenName)}\\s*}}}}";
            return Regex.Replace(
                html,
                pattern,
                _ => replacement,
                RegexOptions.CultureInvariant | RegexOptions.Multiline
            );
        }
    }
}
