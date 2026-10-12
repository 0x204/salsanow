using System.Diagnostics;
using SalsaNOW.Infrastructure;
using static SalsaNOW.Infrastructure.NativeMethods;

namespace SalsaNOW.Desktop;

internal static class ExplorerShell
{
    public static bool IsReady()
    {
        return ProcessRunner.IsRunning("explorer")
            && FindWindow("Progman", null) != IntPtr.Zero
            && FindWindow("Shell_TrayWnd", null) != IntPtr.Zero;
    }

    public static bool IsVisible()
    {
        var explorerIds = new HashSet<uint>();
        foreach (Process process in Process.GetProcessesByName("explorer"))
        {
            using (process)
                explorerIds.Add((uint)process.Id);
        }

        return explorerIds.Count > 0
            && IsExplorerWindow("Progman", explorerIds)
            && IsExplorerWindow("Shell_TrayWnd", explorerIds);
    }

    public static async Task<bool> WaitUntilReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                if (IsReady())
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    if (IsReady())
                        return true;
                }

                await Task.Delay(500, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public static async Task CloseCustomExplorerAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await Task.Delay(500, cancellationToken);
            if (WindowTools.CloseByTitle("CustomExplorer"))
                Log.Info("CustomExplorer has been closed.");
        }
    }

    public static async Task CloseStartHookWindowAsync(CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if (WindowTools.CloseByTitle("StartHookWindow"))
            {
                Log.Info("StartHookWindow has been closed.");
                return;
            }

            await Task.Delay(200, cancellationToken);
        }

        Log.Warn("StartHookWindow not found.");
    }

    private static bool IsExplorerWindow(string windowClass, HashSet<uint> explorerIds)
    {
        IntPtr window = FindWindow(windowClass, null);
        if (window == IntPtr.Zero || !IsWindowVisible(window))
            return false;

        GetWindowThreadProcessId(window, out uint processId);
        return explorerIds.Contains(processId);
    }
}
