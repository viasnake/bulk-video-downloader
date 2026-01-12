using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BulkVideoDownloader.Services;

public static class UrlExpander
{
    private static readonly Regex RangeRegex = new(@"\[(\d+)-(\d+)\]", RegexOptions.Compiled);

    public static IEnumerable<string> Expand(string url)
    {
        var match = RangeRegex.Match(url);
        if (!match.Success)
        {
            yield return url;
            yield break;
        }

        var start = int.Parse(match.Groups[1].Value);
        var end = int.Parse(match.Groups[2].Value);

        for (var i = start; i <= end; i++)
        {
            yield return RangeRegex.Replace(url, i.ToString(), 1);
        }
    }
}
