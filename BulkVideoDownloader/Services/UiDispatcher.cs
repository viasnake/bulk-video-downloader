using System;
using Avalonia.Threading;

namespace BulkVideoDownloader.Services;

public static class UiDispatcher
{
    public static void Post(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action);
    }
}
