namespace ProcurementHTE.Web.Utils;

public static class IndonesianPhoneNumberFormatter
{
    public static string? NormalizeForStorage(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var digitsOnly = new string(raw.Where(char.IsDigit).ToArray());
        if (digitsOnly.StartsWith("62"))
            digitsOnly = digitsOnly[2..];
        if (digitsOnly.StartsWith("0"))
            digitsOnly = digitsOnly[1..];

        return string.IsNullOrEmpty(digitsOnly) ? null : $"+62{digitsOnly}";
    }

    public static string NormalizeForStorageOrEmpty(string? raw) =>
        NormalizeForStorage(raw) ?? string.Empty;

    public static string? FormatForInput(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return null;

        var normalized = stored.Trim();
        if (normalized.StartsWith("+62"))
            normalized = normalized[3..];
        else if (normalized.StartsWith("62"))
            normalized = normalized[2..];
        if (normalized.StartsWith("0"))
            normalized = normalized[1..];

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
