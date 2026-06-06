using System.Text;
using System.Text.RegularExpressions;

namespace ProcurementHTE.Web.Utils;

public static class ProcurementReferenceNumberFormatter
{
    private const string ReferenceNumberPrefix = "/PDC-1110/";
    private const string ReferenceNumberSuffixEnd = "-S0";

    public static string? AppendSuffixIfNeeded(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var trimmed = value.Trim();

        if (HasReferenceNumberSuffix(trimmed))
            return trimmed;

        return trimmed + GetCurrentYearSuffix();
    }

    public static string? RemoveSuffixIfNeeded(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var trimmed = value.Trim();
        var pattern = $"{ReferenceNumberPrefix}\\d{{4}}{ReferenceNumberSuffixEnd}$";
        return Regex.Replace(trimmed, pattern, "");
    }

    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var ch in fileName.Trim())
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }

        return sb.ToString();
    }

    private static string GetCurrentYearSuffix() =>
        $"{ReferenceNumberPrefix}{DateTime.Now.Year}{ReferenceNumberSuffixEnd}";

    private static bool HasReferenceNumberSuffix(string value)
    {
        var pattern = $"{ReferenceNumberPrefix}\\d{{4}}{ReferenceNumberSuffixEnd}$";
        return Regex.IsMatch(value, pattern);
    }
}
