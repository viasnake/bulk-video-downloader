using BulkVideoDownloader.Models;

namespace BulkVideoDownloader.ViewModels;

public sealed class DownloadItemViewModel : ObservableObject
{
    private DownloadStatus _status;
    private double _progress;
    private string _outputFile = string.Empty;
    private string _errorMessage = string.Empty;

    public DownloadItemViewModel(string url)
    {
        Url = url;
        _status = DownloadStatus.Waiting;
    }

    public string Url { get; }

    public DownloadStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => Status switch
    {
        DownloadStatus.Waiting => "待機",
        DownloadStatus.Running => "実行中",
        DownloadStatus.Completed => "完了",
        DownloadStatus.Error => "エラー",
        _ => "不明"
    };

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string OutputFile
    {
        get => _outputFile;
        private set => SetProperty(ref _outputFile, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void Reset()
    {
        Status = DownloadStatus.Waiting;
        Progress = 0;
        OutputFile = string.Empty;
        ErrorMessage = string.Empty;
    }

    public void SetRunning()
    {
        Status = DownloadStatus.Running;
    }

    public void SetCompleted()
    {
        Status = DownloadStatus.Completed;
        Progress = 100;
    }

    public void SetError(string message)
    {
        Status = DownloadStatus.Error;
        ErrorMessage = message;
    }

    public void UpdateProgress(double progress)
    {
        Progress = progress;
    }

    public void SetOutputFile(string outputFile)
    {
        OutputFile = outputFile;
    }
}
