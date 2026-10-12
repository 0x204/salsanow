using System.Diagnostics;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Desktop;

internal sealed class DesktopWatchdog(string executable, string workingDirectory)
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(60);

    public void Start()
    {
        using (Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = workingDirectory, UseShellExecute = false }))
        {
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        bool announced = false;
        int failedStarts = 0;

        while (true)
        {
            await Task.Delay(RetryDelay(failedStarts), cancellationToken);

            try
            {
                if (ExplorerShell.IsVisible())
                {
                    failedStarts = 0;
                    if (!announced)
                    {
                        Log.Info("Desktop is available.");
                        announced = true;
                    }

                    continue;
                }

                announced = false;
                failedStarts = Math.Min(failedStarts + 1, 4);
                Log.Warn("Desktop is not available. Starting " + executable);
                Start();
            }
            catch (Exception ex)
            {
                Log.Error("Desktop start failed", ex);
            }
        }
    }

    private static TimeSpan RetryDelay(int failedStarts)
    {
        var delay = TimeSpan.FromTicks(CheckInterval.Ticks << failedStarts);
        return delay < MaxRetryDelay ? delay : MaxRetryDelay;
    }
}
