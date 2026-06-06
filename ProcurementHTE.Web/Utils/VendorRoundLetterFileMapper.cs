using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace ProcurementHTE.Web.Utils;

public static class VendorRoundLetterFileMapper
{
    private static readonly Regex LetterFileFieldRegex = new(
        "^Vendors\\[(\\d+)\\]\\.LetterFiles\\[(\\d+)\\]$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );

    public static Dictionary<int, Dictionary<int, IFormFile>> BuildLookup(
        IFormFileCollection? files
    )
    {
        var lookup = new Dictionary<int, Dictionary<int, IFormFile>>();
        if (files == null || files.Count == 0)
            return lookup;

        foreach (var formFile in files)
        {
            if (formFile == null || string.IsNullOrWhiteSpace(formFile.Name))
                continue;

            var match = LetterFileFieldRegex.Match(formFile.Name);
            if (!match.Success)
                continue;

            if (!int.TryParse(match.Groups[1].Value, out var vendorIndex))
                continue;

            if (!int.TryParse(match.Groups[2].Value, out var roundIndex))
                continue;

            if (!lookup.TryGetValue(vendorIndex, out var roundMap))
            {
                roundMap = new Dictionary<int, IFormFile>();
                lookup[vendorIndex] = roundMap;
            }

            roundMap[roundIndex] = formFile;
        }

        return lookup;
    }

    public static List<IFormFile?> Merge(
        List<IFormFile?>? boundFiles,
        Dictionary<int, Dictionary<int, IFormFile>> lookup,
        int vendorIndex
    )
    {
        var files = boundFiles is { Count: > 0 }
            ? new List<IFormFile?>(boundFiles)
            : new List<IFormFile?>();

        if (!lookup.TryGetValue(vendorIndex, out var roundFiles) || roundFiles.Count == 0)
        {
            return files;
        }

        var requiredLength =
            roundFiles.Keys.Count > 0
                ? Math.Max(files.Count, roundFiles.Keys.Max() + 1)
                : files.Count;

        while (files.Count < requiredLength)
        {
            files.Add(null);
        }

        foreach (var kvp in roundFiles)
        {
            var roundIndex = kvp.Key;
            if (roundIndex < 0)
                continue;

            if (roundIndex >= files.Count)
            {
                while (files.Count <= roundIndex)
                {
                    files.Add(null);
                }
            }

            files[roundIndex] = kvp.Value;
        }

        return files;
    }
}
