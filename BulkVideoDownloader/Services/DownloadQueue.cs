using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BulkVideoDownloader.Models;
using BulkVideoDownloader.ViewModels;

namespace BulkVideoDownloader.Services;

public sealed class DownloadQueue
{
    private readonly DownloadService _downloadService = new();

    public async Task RunAsync(
        IReadOnlyCollection<DownloadItemViewModel> items,
        SettingsModel settings,
        Action<string> log,
        CancellationToken cancellationToken)
    {
        var parallelism = Math.Max(1, settings.Parallelism);
        using var semaphore = new SemaphoreSlim(parallelism, parallelism);
        var tasks = new List<Task>();

        foreach (var item in items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            UiDispatcher.Post(item.Reset);
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            tasks.Add(RunItemAsync(item, settings, log, semaphore, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task RunItemAsync(
        DownloadItemViewModel item,
        SettingsModel settings,
        Action<string> log,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        try
        {
            await _downloadService.DownloadAsync(item, settings, log, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            UiDispatcher.Post(() => item.SetError("停止しました。"));
        }
        catch (Exception ex)
        {
            UiDispatcher.Post(() => item.SetError("予期しないエラーが発生しました。"));
            log($"ダウンロード中の例外: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
    }
}
