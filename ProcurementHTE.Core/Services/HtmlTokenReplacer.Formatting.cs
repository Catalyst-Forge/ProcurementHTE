using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services
{
    public partial class HtmlTokenReplacer
    {
        private static string FormatDate(DateTime? value)
        {
            return HtmlTokenFormatter.FormatDate(value);
        }

        private static string FormatDecimal(decimal? value, string format = "N0")
        {
            return HtmlTokenFormatter.FormatDecimal(value, format);
        }

        private static string FormatCurrency(decimal? value, string format = "C0")
        {
            return HtmlTokenFormatter.FormatCurrency(value, format);
        }

        private static string GetUserName(User? user)
        {
            return HtmlTokenFormatter.GetUserName(user);
        }
    }
}
