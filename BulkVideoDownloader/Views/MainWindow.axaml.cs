using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BulkVideoDownloader.ViewModels;

namespace BulkVideoDownloader.Views;

public sealed partial class MainWindow : Window
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
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        await ViewModel.SaveSettingsAsync();
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
