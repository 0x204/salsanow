using System.ComponentModel;
using System.Diagnostics;

namespace SalsaNOW.Infrastructure;

internal static class ProcessRunner
{
    public static int Run(string fileName, string arguments, TimeSpan? timeout = null)
    {
        using Process process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        }) ?? throw new InvalidOperationException($"{fileName} did not start.");

        if (timeout is null)
        {
            process.WaitForExit();
        }
        else if (!process.WaitForExit((int)timeout.Value.TotalMilliseconds))
        {
            TryKill(process);
            throw new TimeoutException($"{Path.GetFileName(fileName)} did not finish within {timeout.Value.TotalSeconds:0} seconds.");
        }

        return process.ExitCode;
    }

    public static Process? Launch(string path, string arguments = "", string? workingDirectory = null)
    {
        return Process.Start(new ProcessStartInfo(path, arguments)
        {
            UseShellExecute = true,
            WorkingDirectory = workingDirectory ?? "",
        });
    }

    public static Process? StartHidden(string path, string? workingDirectory = null)
    {
        return Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = workingDirectory ?? "",
        });
    }

    public static int KillAll(string processName, TimeSpan? waitForExit = null)
    {
        int killed = 0;
        foreach (Process process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                if (!TryKill(process))
                    continue;

                killed++;
                if (waitForExit is { } wait)
                    process.WaitForExit((int)wait.TotalMilliseconds);
            }
        }

        return killed;
    }

    public static bool IsRunning(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        foreach (Process process in processes)
            process.Dispose();

        return processes.Length > 0;
    }

    public static bool TryKill(Process process)
    {
        try
        {
            if (process.HasExited)
                return false;

            process.Kill();
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
