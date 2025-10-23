using System.Text.RegularExpressions;

namespace ProcurementHTE.Core.Services
{
    public static class SlugFolderHelper
    {
        public static string SlugFolder(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "unknown";
            var s = name.Trim();
            s = Regex.Replace(s, @"\s+", "-");
            s = Regex.Replace(s, @"[^A-Za-z0-9_\-\.]+", "");
            return string.IsNullOrWhiteSpace(s) ? "unknown" : s;
        }
        public static string MakeSafeFilename(string fileName)
        {
            var name = Path.GetFileName(fileName).Trim();
            name = Regex.Replace(name, @"\s+", "_");
            name = Regex.Replace(name, @"[^A-Za-z0-9_\-\.]+", "");
            return string.IsNullOrWhiteSpace(name) ? $"file_{DateTime.UtcNow:yyyyMMddHHmmss}.bin" : name;
        }
    }
}
