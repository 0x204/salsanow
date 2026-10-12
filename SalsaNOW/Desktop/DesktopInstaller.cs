using SalsaNOW.Apps;
using SalsaNOW.Configuration;
using SalsaNOW.Core.Apps;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;
using SalsaNOW.Shell;

namespace SalsaNOW.Desktop;

internal sealed class DesktopInstaller(SalsaPaths paths, RemoteConfig config, bool bingWallpaper)
{
    private const string ClassicContextMenuKey = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";

    public async Task InstallAsync(CancellationToken cancellationToken)
    {
        TryWithLog("Failed to set Dark Mode", Appearance.WriteDarkTheme);

        string? wallpaper = await Wallpaper.ChooseAsync(paths, cancellationToken);
        if (wallpaper is not null)
            Wallpaper.Apply(wallpaper);

        try
        {
            List<DesktopEntry> desktops = await config.LoadAsync<List<DesktopEntry>>(Endpoints.Desktops, cancellationToken);

            ProcessRunner.KillAll("CustomExplorer");

            foreach (DesktopEntry desktop in desktops)
                await InstallAsync(desktop, cancellationToken);

            if (bingWallpaper)
                wallpaper = await Wallpaper.DownloadBingImageAsync(paths.Wallpapers, cancellationToken) ?? wallpaper;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("Desktop setup failed", ex);
        }

        if (wallpaper is not null)
            await Wallpaper.ApplyWhenDesktopReadyAsync(wallpaper, cancellationToken);
    }

    private async Task InstallAsync(DesktopEntry desktop, CancellationToken cancellationToken)
    {
        string directory = paths.InRoot(desktop.Name);
        try
        {
            var marker = new VersionMarker(Path.Combine(directory, ".version"));
            if (!Directory.Exists(directory) || marker.Read() != desktop.ReleaseFileName)
            {
                await ZipPackage.InstallAsync(desktop.Url, directory, cancellationToken);
                marker.Write(desktop.ReleaseFileName);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error($"Could not install {desktop.Name}", ex);
        }

        if (!desktop.Run)
            return;

        string executable = Path.Combine(directory, desktop.ExeName);
        Log.Info("Starting desktop app: " + executable);
        TryWithLog("Classic right-click menu failed", EnableClassicContextMenu);

        var watchdog = new DesktopWatchdog(executable, directory);
        watchdog.Start();
        BackgroundJobs.Start("Desktop watchdog", () => watchdog.RunAsync(cancellationToken));
    }

    private static void EnableClassicContextMenu()
    {
        UserRegistry.SetString(ClassicContextMenuKey, "", "");
        Log.Info("Classic right-click menu enabled.");
    }

    private static void TryWithLog(string failureMessage, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            Log.Error(failureMessage, ex);
        }
    }
}
