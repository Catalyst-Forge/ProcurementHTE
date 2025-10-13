namespace ProcurementHTE.Core.Utils
{
    public static class SequenceNumberGenerator
    {
        public static string NumId(string prefix, string? lastId)
        {
            var next = 1;
            if (
                !string.IsNullOrWhiteSpace(lastId)
                && lastId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            )
            {
                var numPart = lastId[prefix.Length..];
                if (int.TryParse(numPart, out var lastNum))
                {
                    next = lastNum + 1;
                }
            }

            return $"{prefix}{next.ToString("D6")}";
        }
    }
}
