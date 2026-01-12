using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BulkVideoDownloader.Views;

namespace BulkVideoDownloader;

public sealed class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        RegisterExceptionHandlers();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            LogException("AppDomain", args.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogException("TaskScheduler", args.Exception);
            args.SetObserved();
        };

    }

    private static void LogException(string source, Exception? exception)
    {
        if (exception is null)
        {
            return;
        }

        try
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BulkVideoDownloader",
                "logs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "app.log");
            var message = $"{DateTimeOffset.Now:O} [{source}] {exception}{Environment.NewLine}";
            File.AppendAllText(logPath, message);
        }
        catch
        {
        }
    }
}
