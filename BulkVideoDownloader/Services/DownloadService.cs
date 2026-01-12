using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BulkVideoDownloader.Models;
using BulkVideoDownloader.ViewModels;

namespace BulkVideoDownloader.Services;

public sealed class DownloadService
{
    private static readonly Regex ProgressRegex = new(@"(?<percent>\d{1,3}(?:\.\d+)?)%", RegexOptions.Compiled);
    private static readonly Regex DestinationRegex = new(@"Destination:\s*(.+)", RegexOptions.Compiled);

    public async Task DownloadAsync(
        DownloadItemViewModel item,
        SettingsModel settings,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        var process = new Process
        {
            StartInfo = BuildStartInfo(item.Url, settings)
        };

        void HandleOutput(string? data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return;
            }

            log(data);
            if (TryParseProgress(data, out var percent))
            {
                UiDispatcher.Post(() => item.UpdateProgress(percent));
            }

            var destinationMatch = DestinationRegex.Match(data);
            if (destinationMatch.Success)
            {
                UiDispatcher.Post(() => item.SetOutputFile(destinationMatch.Groups[1].Value));
            }
        }

        process.OutputDataReceived += (_, args) => HandleOutput(args.Data);
        process.ErrorDataReceived += (_, args) => HandleOutput(args.Data);

        try
        {
            if (!process.Start())
            {
                UiDispatcher.Post(() => item.SetError("yt-dlp の起動に失敗しました。"));
                log("yt-dlp の起動に失敗しました。");
                return;
            }
        }
        catch (Exception ex)
        {
            UiDispatcher.Post(() => item.SetError("yt-dlp を起動できませんでした。"));
            log($"yt-dlp 起動エラー: {ex.Message}");
            return;
        }

        using var registration = cancellationToken.Register(() => TryKill(process));

        UiDispatcher.Post(item.SetRunning);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            UiDispatcher.Post(() => item.SetError("停止しました。"));
            return;
        }

        if (process.ExitCode == 0)
        {
            UiDispatcher.Post(item.SetCompleted);
        }
        else
        {
            UiDispatcher.Post(() => item.SetError($"失敗しました (ExitCode: {process.ExitCode})"));
        }
    }

    private static ProcessStartInfo BuildStartInfo(string url, SettingsModel settings)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveYtDlpPath(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var arguments = new List<string>();
        if (!string.IsNullOrWhiteSpace(settings.OutputDirectory))
        {
            arguments.Add("-P");
            arguments.Add(settings.OutputDirectory);
        }

        if (!string.IsNullOrWhiteSpace(settings.AdditionalOptions))
        {
            arguments.AddRange(CommandLineSplitter.Split(settings.AdditionalOptions));
        }

        arguments.Add("--newline");
        arguments.Add(url);

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static string ResolveYtDlpPath()
    {
        return YtDlpBootstrapper.ResolvePath();
    }

    private static bool TryParseProgress(string line, out double percent)
    {
        percent = 0;
        var match = ProgressRegex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        if (!double.TryParse(match.Groups["percent"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out percent))
        {
            return false;
        }

        if (percent < 0)
        {
            percent = 0;
        }

        if (percent > 100)
        {
            percent = 100;
        }

        return true;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
