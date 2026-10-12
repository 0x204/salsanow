using SalsaNOW.Configuration;
using SalsaNOW.Core.Apps;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;
using SalsaNOW.Shortcuts;

namespace SalsaNOW.Apps;

internal sealed class AppInstaller(SalsaPaths paths, RemoteConfig config, string? customAppsJson)
{
    public async Task InstallAsync(CancellationToken cancellationToken)
    {
        List<AppEntry> apps;
        try
        {
            apps = await AppCatalog.LoadAsync(config, customAppsJson, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("Could not load the app list", ex);
            return;
        }

        await Task.WhenAll(apps.Select(app => InstallAsync(app, cancellationToken)));
    }

    private async Task InstallAsync(AppEntry app, CancellationToken cancellationToken)
    {
        try
        {
            if (app.IsExecutable)
                await InstallExecutableAsync(app, cancellationToken);
            else if (app.IsArchive)
                await InstallArchiveAsync(app, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error($"Could not install {app.Name}", ex);
        }
    }

    private async Task InstallExecutableAsync(AppEntry app, CancellationToken cancellationToken)
    {
        string executable = paths.InRoot(app.ExeName);

        Log.Info("Downloading " + app.Name);
        try
        {
            await Http.DownloadFileAsync(app.Url, executable, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && File.Exists(executable))
        {
            Log.Warn($"Could not download {app.Name} ({ex.Message}). Using the installed copy.");
        }

        CreateDesktopShortcut(app.Name, executable, paths.Root);

        if (app.Run)
            ProcessRunner.Launch(executable, workingDirectory: paths.Root)?.Dispose();
    }

    private async Task InstallArchiveAsync(AppEntry app, CancellationToken cancellationToken)
    {
        string directory = paths.InRoot(app.Name);
        string executable = Path.Combine(directory, app.ExeName);
        string version = PackageVersion.Normalize(app.Version);
        var marker = new VersionMarker(Path.Combine(directory, ".version"));

        bool installed = File.Exists(executable);
        bool updateAvailable = PackageVersion.IsUpdateAvailable(version, marker.Read());

        if (installed && !updateAvailable)
        {
            Log.Info($"{app.Name} already exists. Skipping download and respecting user desktop layout.");
        }
        else
        {
            Log.Info(installed ? $"Updating {app.Name} to {version}" : "Installing " + app.Name);
            await ZipPackage.InstallAsync(app.Url, directory, cancellationToken);
            marker.Write(version);
            CreateDesktopShortcut(app.Name, executable, directory);
        }

        if (app.Run)
            ProcessRunner.Launch(executable, workingDirectory: directory)?.Dispose();
    }

    private void CreateDesktopShortcut(string name, string target, string workingDirectory)
    {
        string shortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name + ".lnk");
        if (File.Exists(shortcut) || !ShortcutSync.IsKnown(paths, Path.GetFileName(shortcut)))
            ShellLink.Create(shortcut, target, workingDirectory);
    }
}
