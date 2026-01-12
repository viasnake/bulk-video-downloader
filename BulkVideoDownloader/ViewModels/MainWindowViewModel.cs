using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BulkVideoDownloader.Models;
using BulkVideoDownloader.Services;

namespace BulkVideoDownloader.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly DownloadQueue _downloadQueue = new();
    private readonly SettingsService _settingsService = new();
    private readonly Queue<string> _logBuffer = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private string _urlInput = string.Empty;
    private string _outputDirectory = string.Empty;
    private string _additionalOptions = string.Empty;
    private int _parallelism = 1;
    private string _logText = string.Empty;
    private bool _isRunning;

    public MainWindowViewModel()
    {
        Items.CollectionChanged += OnItemsChanged;
        AddUrlsCommand = new RelayCommand(AddUrlsFromInput, () => !string.IsNullOrWhiteSpace(UrlInput));
        StartCommand = new AsyncRelayCommand(StartAsync, CanStart);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
    }

    public ObservableCollection<DownloadItemViewModel> Items { get; } = new();

    public RelayCommand AddUrlsCommand { get; }

    public AsyncRelayCommand StartCommand { get; }

    public RelayCommand StopCommand { get; }

    public string UrlInput
    {
        get => _urlInput;
        set
        {
            if (SetProperty(ref _urlInput, value))
            {
                AddUrlsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    public string AdditionalOptions
    {
        get => _additionalOptions;
        set => SetProperty(ref _additionalOptions, value);
    }

    public int Parallelism
    {
        get => _parallelism;
        set
        {
            var normalized = Math.Max(1, value);
            SetProperty(ref _parallelism, normalized);
        }
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync().ConfigureAwait(false);
        UiDispatcher.Post(() =>
        {
            OutputDirectory = settings.OutputDirectory;
            AdditionalOptions = settings.AdditionalOptions;
            Parallelism = settings.Parallelism;
        });
    }

    public async Task SaveSettingsAsync()
    {
        var settings = BuildSettings();
        await _settingsService.SaveAsync(settings).ConfigureAwait(false);
    }

    public void AddUrlsFromLines(IEnumerable<string> lines)
    {
        var urls = lines
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        foreach (var url in urls)
        {
            foreach (var expanded in UrlExpander.Expand(url))
            {
                Items.Add(new DownloadItemViewModel(expanded));
            }
        }

        AddUrlsCommand.RaiseCanExecuteChanged();
        StartCommand.RaiseCanExecuteChanged();
    }

    public async Task AddUrlsFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            AppendLog($"ファイルが見つかりません: {filePath}");
            return;
        }

        var lines = await File.ReadAllLinesAsync(filePath).ConfigureAwait(false);
        UiDispatcher.Post(() => AddUrlsFromLines(lines));
    }

    public void AppendLog(string message)
    {
        UiDispatcher.Post(() =>
        {
            _logBuffer.Enqueue(message);
            while (_logBuffer.Count > 500)
            {
                _logBuffer.Dequeue();
            }

            LogText = string.Join(Environment.NewLine, _logBuffer);
        });
    }

    private void AddUrlsFromInput()
    {
        var lines = UrlInput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        AddUrlsFromLines(lines);
        UrlInput = string.Empty;
    }

    private async Task StartAsync()
    {
        if (IsRunning || Items.Count == 0)
        {
            return;
        }

        IsRunning = true;
        AppendLog("ダウンロードを開始しました。");
        _cancellationTokenSource = new CancellationTokenSource();

        await SaveSettingsAsync().ConfigureAwait(false);

        try
        {
            await _downloadQueue.RunAsync(
                Items,
                BuildSettings(),
                AppendLog,
                _cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            AppendLog("ダウンロードを停止しました。");
        }
        catch (Exception ex)
        {
            AppendLog($"予期しないエラー: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        AppendLog("停止処理を要求しました。");
    }

    private SettingsModel BuildSettings()
    {
        return new SettingsModel
        {
            OutputDirectory = OutputDirectory ?? string.Empty,
            AdditionalOptions = AdditionalOptions ?? string.Empty,
            Parallelism = Parallelism
        };
    }

    private bool CanStart()
    {
        return !IsRunning && Items.Count > 0;
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        StartCommand.RaiseCanExecuteChanged();
    }
}
