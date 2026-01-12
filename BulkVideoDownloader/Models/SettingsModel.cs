using System.Collections.Generic;

namespace BulkVideoDownloader.Models;

public sealed class SettingsModel
{
    public string OutputDirectory { get; set; } = string.Empty;
    public string AdditionalOptions { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Parallelism { get; set; } = 1;
    public Dictionary<string, double> DownloadListColumnWidths { get; set; } = new();
}
