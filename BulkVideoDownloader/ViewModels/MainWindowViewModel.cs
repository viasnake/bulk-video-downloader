using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BulkVideoDownloader.Models;
using BulkVideoDownloader.Services;

namespace BulkVideoDownloader.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly DownloadQueue _downloadQueue = new();
    private readonly SettingsService _settingsService = new();
    private readonly Queue<string> _logBuffer = new();
    private readonly Queue<string> _pendingLogs = new();
    private readonly object _logLock = new();
    private readonly DispatcherTimer _logFlushTimer;
    private CancellationTokenSource? _cancellationTokenSource;
    private string _urlInput = string.Empty;
    private DownloadItemViewModel? _selectedItem;
    private string _outputDirectory = string.Empty;
    private string _additionalOptions = string.Empty;
    private int _parallelism = 1;
    private string _logText = string.Empty;
    private bool _isRunning;

    public MainWindowViewModel()
    {
        Items.CollectionChanged += OnItemsChanged;
        AddUrlsCommand = new RelayCommand(AddUrlsFromInput, () => !string.IsNullOrWhiteSpace(UrlInput));
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, CanRemoveSelected);
        ClearCompletedCommand = new RelayCommand(ClearCompleted, CanClearCompleted);
        StartCommand = new AsyncRelayCommand(StartAsync, CanStart);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        _logFlushTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _logFlushTimer.Tick += (_, _) => FlushLogs();
        _logFlushTimer.Start();
    }

    public ObservableCollection<DownloadItemViewModel> Items { get; } = new();

    public RelayCommand AddUrlsCommand { get; }

    public RelayCommand RemoveSelectedCommand { get; }

    public RelayCommand ClearCompletedCommand { get; }

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

    public DownloadItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                RemoveSelectedCommand.RaiseCanExecuteChanged();
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
                RemoveSelectedCommand.RaiseCanExecuteChanged();
                ClearCompletedCommand.RaiseCanExecuteChanged();
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
        ClearCompletedCommand.RaiseCanExecuteChanged();
    }

    private void RemoveSelected()
    {
        if (SelectedItem is null)
        {
            return;
        }

        Items.Remove(SelectedItem);
        SelectedItem = null;
        StartCommand.RaiseCanExecuteChanged();
    }

    private bool CanRemoveSelected()
    {
        return !IsRunning && SelectedItem is not null;
    }

    private void ClearCompleted()
    {
        var completed = Items.Where(item => item.Status == DownloadStatus.Completed).ToList();
        if (completed.Count == 0)
        {
            return;
        }

        foreach (var item in completed)
        {
            Items.Remove(item);
        }

        if (SelectedItem is not null && !Items.Contains(SelectedItem))
        {
            SelectedItem = null;
        }

        StartCommand.RaiseCanExecuteChanged();
        ClearCompletedCommand.RaiseCanExecuteChanged();
    }

    private bool CanClearCompleted()
    {
        return !IsRunning && Items.Any(item => item.Status == DownloadStatus.Completed);
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
        lock (_logLock)
        {
            _pendingLogs.Enqueue(message);
            while (_pendingLogs.Count > 2000)
            {
                _pendingLogs.Dequeue();
            }
        }
    }

    private void FlushLogs()
    {
        List<string>? pending = null;
        lock (_logLock)
        {
            if (_pendingLogs.Count == 0)
            {
                return;
            }

            pending = new List<string>(_pendingLogs);
            _pendingLogs.Clear();
        }

        foreach (var entry in pending)
        {
            _logBuffer.Enqueue(entry);
            while (_logBuffer.Count > 500)
            {
                _logBuffer.Dequeue();
            }
        }

        LogText = string.Join(Environment.NewLine, _logBuffer);
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

        UiDispatcher.Post(() => IsRunning = true);
        AppendLog("ダウンロードを開始しました。");
        _cancellationTokenSource = new CancellationTokenSource();

        await SaveSettingsAsync();

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
            UiDispatcher.Post(() => IsRunning = false);
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
        return !IsRunning && Items.Any(item => item.Status != DownloadStatus.Completed);
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (DownloadItemViewModel item in e.NewItems)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (DownloadItemViewModel item in e.OldItems)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
        }

        StartCommand.RaiseCanExecuteChanged();
        RemoveSelectedCommand.RaiseCanExecuteChanged();
        ClearCompletedCommand.RaiseCanExecuteChanged();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DownloadItemViewModel.Status))
        {
            ClearCompletedCommand.RaiseCanExecuteChanged();
            StartCommand.RaiseCanExecuteChanged();
        }
    }
}
