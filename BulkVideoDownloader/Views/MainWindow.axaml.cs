using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BulkVideoDownloader.ViewModels;
using SukiUI.Controls;

namespace BulkVideoDownloader.Views;

public sealed partial class MainWindow : SukiWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private async void OnOpened(object? sender, EventArgs e)
    {
        await ViewModel.LoadSettingsAsync();
        Dispatcher.UIThread.Post(ApplyColumnWidths, DispatcherPriority.Loaded);
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        ViewModel.SetDownloadListColumnWidths(CaptureColumnWidths());
        await ViewModel.SaveSettingsAsync();
    }

    private void ApplyColumnWidths()
    {
        if (DownloadGrid is null)
        {
            return;
        }

        var widths = ViewModel.DownloadListColumnWidths;
        if (widths.Count == 0)
        {
            return;
        }

        foreach (var column in DownloadGrid.Columns)
        {
            var key = column.Header?.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (widths.TryGetValue(key, out var width) && width > 0)
            {
                column.Width = new DataGridLength(width, DataGridLengthUnitType.Pixel);
            }
        }
    }

    private Dictionary<string, double> CaptureColumnWidths()
    {
        var widths = new Dictionary<string, double>();
        if (DownloadGrid is null)
        {
            return widths;
        }

        foreach (var column in DownloadGrid.Columns)
        {
            var key = column.Header?.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var width = column.ActualWidth;
            if (width > 0)
            {
                widths[key] = width;
            }
        }

        return widths;
    }

    private async void OpenUrlFile(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "URLファイルを選択"
        });

        var file = files.FirstOrDefault();
        var path = file?.TryGetLocalPath();
        if (path is null)
        {
            return;
        }

        await ViewModel.AddUrlsFromFileAsync(path);
    }

    private async void BrowseOutputFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "保存先フォルダを選択"
        });

        var folder = folders.FirstOrDefault();
        var path = folder?.TryGetLocalPath();
        if (path is null)
        {
            return;
        }

        ViewModel.OutputDirectory = path;
    }
}
