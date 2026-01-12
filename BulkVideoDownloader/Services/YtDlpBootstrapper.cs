using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BulkVideoDownloader.Services;

public sealed class YtDlpBootstrapper
{
    private const string DownloadUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
    private static readonly SemaphoreSlim DownloadLock = new(1, 1);

    public static string ResolvePath()
    {
        var localPath = GetLocalPath();
        return File.Exists(localPath) ? localPath : "yt-dlp";
    }

    public async Task<bool> EnsureAsync(Action<string> log, CancellationToken cancellationToken)
    {
        if (IsYtDlpAvailable())
        {
            return true;
        }

        await DownloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsYtDlpAvailable())
            {
                return true;
            }

            log("yt-dlp を自動取得します。");
            var targetPath = GetLocalPath();
            var tempPath = Path.Combine(Path.GetTempPath(), $"yt-dlp-{Guid.NewGuid():N}.exe");

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(
                DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using (var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await downloadStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Move(tempPath, targetPath, true);
            log($"yt-dlp を取得しました: {targetPath}");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            log($"yt-dlp の自動取得に失敗しました: {ex.Message}");
            return false;
        }
        finally
        {
            DownloadLock.Release();
        }
    }

    private static bool IsYtDlpAvailable()
    {
        if (File.Exists(GetLocalPath()))
        {
            return true;
        }

        var fileName = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";
        return ExistsOnPath(fileName);
    }

    private static bool ExistsOnPath(string fileName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return false;
        }

        var paths = pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var path in paths)
        {
            var candidate = Path.Combine(path, fileName);
            if (File.Exists(candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetLocalPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "yt-dlp.exe");
    }
}
