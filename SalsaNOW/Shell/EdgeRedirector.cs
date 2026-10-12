using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using SalsaNOW.Configuration;
using SalsaNOW.Core.IO;
using SalsaNOW.Core.Settings;
using SalsaNOW.Core.Web;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Shell;

internal sealed class EdgeRedirector(UserSettingsFile settingsFile, string fallbackBrowser)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly LaunchThrottle _throttle = new(TimeSpan.FromSeconds(3));
    private readonly HashSet<int> _inspectedProcesses = [];
    private DateTime _settingsWriteTime = DateTime.MinValue;
    private string? _browser;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _browser = fallbackBrowser;
        RefreshBrowser();
        LogBrowser(changed: false);

        while (true)
        {
            try
            {
                string? previous = _browser;
                RefreshBrowser();
                if (!string.Equals(previous, _browser, StringComparison.OrdinalIgnoreCase))
                    LogBrowser(changed: true);

                if (IsUsable(_browser))
                    RedirectNewEdgeProcesses(_browser!);
            }
            catch (Exception)
            {
            }

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    private void RedirectNewEdgeProcesses(string browser)
    {
        HashSet<int> running = RunningEdgeProcesses();
        _inspectedProcesses.IntersectWith(running);
        if (running.IsSubsetOf(_inspectedProcesses))
            return;

        var urls = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'msedge.exe'"))
        using (ManagementObjectCollection processes = searcher.Get())
        {
            foreach (ManagementObject process in processes)
            {
                using (process)
                {
                    int processId = Convert.ToInt32(process["ProcessId"]);
                    if (!_inspectedProcesses.Add(processId))
                        continue;

                    string? url = UrlText.FindNavigationUrl(process["CommandLine"] as string);
                    if (url is null)
                        continue;

                    if (!urls.TryGetValue(url, out List<int>? processIds))
                        urls[url] = processIds = [];
                    processIds.Add(processId);
                }
            }
        }

        DateTime now = DateTime.UtcNow;
        foreach (KeyValuePair<string, List<int>> entry in urls)
        {
            foreach (int processId in entry.Value)
                Kill(processId);

            if (!_throttle.TryBegin(entry.Key, now))
                continue;

            Log.Info("Intercepted Edge URL to " + UrlText.HostOf(entry.Key));
            OpenInBrowser(browser, entry.Key);
        }
    }

    private static HashSet<int> RunningEdgeProcesses()
    {
        var ids = new HashSet<int>();
        foreach (Process process in Process.GetProcessesByName("msedge"))
        {
            using (process)
                ids.Add(process.Id);
        }

        return ids;
    }

    private static void Kill(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            ProcessRunner.TryKill(process);
        }
        catch (ArgumentException)
        {
        }
    }

    private static void OpenInBrowser(string browser, string url)
    {
        try
        {
            using (Process.Start(new ProcessStartInfo(browser, CommandLine.Quote(url)) { UseShellExecute = false }))
            {
            }

            Log.Info("Launched default browser with URL.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Log.Error("Failed to launch default browser", ex);
        }
    }

    private void RefreshBrowser()
    {
        if (!settingsFile.Exists)
        {
            _settingsWriteTime = DateTime.MinValue;
            _browser = fallbackBrowser;
            return;
        }

        DateTime writeTime = settingsFile.LastWriteTimeUtc;
        if (writeTime == _settingsWriteTime)
            return;

        UserSettings? settings = settingsFile.TryRead();
        if (settings is null)
            return;

        _settingsWriteTime = writeTime;
        _browser = string.IsNullOrWhiteSpace(settings.DefaultBrowser) ? fallbackBrowser : settings.DefaultBrowser;
    }

    private static bool IsUsable(string? browser)
    {
        return !string.IsNullOrWhiteSpace(browser)
            && !browser!.EndsWith("msedge.exe", StringComparison.OrdinalIgnoreCase)
            && File.Exists(browser);
    }

    private void LogBrowser(bool changed)
    {
        if (IsUsable(_browser))
            Log.Info((changed ? "Default browser changed: " : "Edge interceptor running. Default browser: ") + _browser);
        else if (string.IsNullOrWhiteSpace(_browser))
            Log.Warn(changed ? "Default browser was cleared. Edge URLs will not be redirected." : "Default browser is not set. Edge URLs will not be redirected.");
        else if (_browser!.EndsWith("msedge.exe", StringComparison.OrdinalIgnoreCase))
            Log.Warn("Default browser is Edge. Edge URLs will not be redirected.");
        else
            Log.Warn("Default browser not found: " + _browser);
    }
}
