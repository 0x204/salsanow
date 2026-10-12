using SalsaNOW.Configuration;
using SalsaNOW.Core.Settings;
using SalsaNOW.Desktop;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Shell;

internal sealed class ShellSettings(SalsaPaths paths, UserSettingsFile settingsFile)
{
    private readonly TranslucentTb _translucentTb = new(paths.TranslucentTb);
    private DateTime _appliedWriteTime = DateTime.MinValue;

    public void Start(CancellationToken cancellationToken)
    {
        BackgroundJobs.Start("Edge redirect", () => new EdgeRedirector(settingsFile, paths.Waterfox).RunAsync(cancellationToken));

        Try("defaults", () =>
        {
            Appearance.ApplyDefaults();
            Log.Info("Default dark theme and left taskbar alignment applied.");
        });

        DateTime writeTime = settingsFile.Exists ? settingsFile.LastWriteTimeUtc : DateTime.MinValue;
        UserSettings? settings = settingsFile.TryRead();
        if (settings is not null)
        {
            ApplyStartupSettings(settings, cancellationToken);
            _appliedWriteTime = writeTime;
        }
        else
        {
            BackgroundJobs.Start("Default appearance", () => RefreshWhenDesktopReadyAsync(cancellationToken));
        }

        BackgroundJobs.Start("Taskbar theme refresh", async () =>
        {
            if (!await ExplorerShell.WaitUntilReadyAsync(cancellationToken))
                return;

            for (int i = 0; i < 4; i++)
            {
                RefreshShellAppearance();
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        });

        BackgroundJobs.Start("Settings watcher", () => WatchSettingsAsync(cancellationToken));
    }

    public void RefreshShellAppearance()
    {
        UserSettings? settings = settingsFile.TryRead();
        if (settings is not null)
        {
            Appearance.WriteThemeMode(settings);
            if (string.IsNullOrWhiteSpace(settings.ColorMode))
                Appearance.WriteDarkTheme();
            if (string.IsNullOrWhiteSpace(settings.TaskbarAlignment))
                Appearance.SetTaskbarAlignment(left: true);
        }
        else
        {
            Appearance.ApplyDefaults();
        }

        Appearance.NotifyThemeChanged();
        Appearance.NotifyTaskbarChanged();
    }

    private void ApplyStartupSettings(UserSettings settings, CancellationToken cancellationToken)
    {
        Log.Info("Applying SalsaNOWSettings.json.");
        Try("colors", () =>
        {
            Appearance.WriteThemeMode(settings);
            Appearance.WriteAccent(settings);
            Log.Info("SalsaNOWSettings.json colors applied.");
        });
        ApplyTaskbar(settings);
        bool startTranslucentTb = _translucentTb.Prepare(settings);
        Try("associations", () => FileAssociations.ApplyFromSettings(settings));

        if (ExplorerShell.IsReady())
        {
            ApplyToRunningDesktop(settings, startTranslucentTb);
            return;
        }

        BackgroundJobs.Start("SalsaNOWSettings.json desktop appearance", async () =>
        {
            if (await ExplorerShell.WaitUntilReadyAsync(cancellationToken))
                ApplyToRunningDesktop(settings, startTranslucentTb);
        });
    }

    private void ApplyToRunningDesktop(UserSettings settings, bool startTranslucentTb)
    {
        Try("theme refresh", () =>
        {
            Appearance.WriteThemeMode(settings);
            Appearance.NotifyThemeChanged();
            Log.Info("SalsaNOWSettings.json light or dark theme refreshed.");
        });
        ApplyTaskbar(settings);

        if (startTranslucentTb && ExplorerShell.IsReady())
            _translucentTb.Start();
    }

    private static void ApplyTaskbar(UserSettings settings)
    {
        Try("taskbar", () =>
        {
            Appearance.WriteTaskbar(settings);
            Appearance.NotifyTaskbarChanged();
            Log.Info("SalsaNOWSettings.json taskbar applied.");
        });
    }

    private async Task RefreshWhenDesktopReadyAsync(CancellationToken cancellationToken)
    {
        if (await ExplorerShell.WaitUntilReadyAsync(cancellationToken))
            RefreshShellAppearance();
    }

    private async Task WatchSettingsAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                ApplyChangedAssociations();
            }
            catch (Exception ex)
            {
                Log.Error("SalsaNOWSettings.json associations failed", ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private void ApplyChangedAssociations()
    {
        if (!settingsFile.Exists)
            return;

        DateTime writeTime = settingsFile.LastWriteTimeUtc;
        if (writeTime == _appliedWriteTime || settingsFile.TryRead() is not { } settings)
            return;

        _appliedWriteTime = writeTime;
        Log.Info("SalsaNOWSettings.json changed. Applying file associations.");
        FileAssociations.ApplyFromSettings(settings);
    }

    private static void Try(string what, Action apply)
    {
        try
        {
            apply();
        }
        catch (Exception ex)
        {
            Log.Error($"SalsaNOWSettings.json {what} failed", ex);
        }
    }
}
