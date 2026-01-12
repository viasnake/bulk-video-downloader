using System.Collections.Generic;
using System.Text;

namespace BulkVideoDownloader.Services;

public static class CommandLineSplitter
{
    public static IReadOnlyList<string> Split(string commandLine)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return results;
        }

        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in commandLine)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    results.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
        {
            results.Add(current.ToString());
        }

        return results;
    }
}
