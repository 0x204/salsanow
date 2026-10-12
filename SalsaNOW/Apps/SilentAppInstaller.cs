using System.Diagnostics;
using SalsaNOW.Configuration;
using SalsaNOW.Core.Apps;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;

namespace SalsaNOW.Apps;

internal sealed class SilentAppInstaller(SalsaPaths paths, RemoteConfig config)
{
    public async Task InstallAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(paths.SilentApps);

        List<SilentAppEntry> apps;
        try
        {
            apps = await config.LoadAsync<List<SilentAppEntry>>(Endpoints.SilentApps, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("Could not load the silent app list", ex);
            return;
        }

        await RemoveRetiredAppsAsync(apps);
        await Task.WhenAll(apps.Select(app => InstallAsync(app, cancellationToken)));
    }

    private async Task RemoveRetiredAppsAsync(List<SilentAppEntry> apps)
    {
        var folders = new HashSet<string>(apps.Where(app => app.Archive).Select(app => app.Name), StringComparer.OrdinalIgnoreCase);
        var files = new HashSet<string>(
            apps.Where(app => IsExtension(app, "exe") || IsExtension(app, "bat")).Select(app => app.ExecutableName),
            StringComparer.OrdinalIgnoreCase);

        foreach (string folder in Directory.GetDirectories(paths.SilentApps).Where(folder => !folders.Contains(Path.GetFileName(folder))))
        {
            try
            {
                await FileSystem.DeleteDirectoryAsync(folder);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warn($"Could not remove the retired silent app folder {Path.GetFileName(folder)}: {ex.Message}");
            }
        }

        foreach (string file in Directory.GetFiles(paths.SilentApps))
        {
            string name = Path.GetFileName(file);
            if (!name.EndsWith(".version", StringComparison.OrdinalIgnoreCase) && !files.Contains(name))
                FileSystem.TryDeleteFile(file);
        }
    }

    private async Task InstallAsync(SilentAppEntry app, CancellationToken cancellationToken)
    {
        try
        {
            string folder = Path.Combine(paths.SilentApps, app.Name);
            string executable = app.Archive
                ? Path.Combine(folder, app.ExecutableName)
                : Path.Combine(paths.SilentApps, app.ExecutableName);
            var marker = new VersionMarker(app.Archive
                ? Path.Combine(folder, ".version")
                : Path.Combine(paths.SilentApps, app.FileName + ".version"));
            string version = PackageVersion.Normalize(app.Version);

            bool installed = File.Exists(executable);
            bool updateAvailable = PackageVersion.IsUpdateAvailable(version, marker.Read());

            bool reinstall = string.Equals(app.Name, "Open-Shell", StringComparison.OrdinalIgnoreCase);

            if (installed && !updateAvailable && !reinstall)
            {
                if (app.Run)
                    Start(executable, app.Archive);
                return;
            }

            if (!installed)
                Log.Info("Installing silent app " + app.Name);
            else if (updateAvailable)
                Log.Info($"Updating silent app {app.Name} to {version}");

            if (app.Archive)
                await ZipPackage.InstallAsync(app.Url, folder, cancellationToken);
            else
                await Http.DownloadFileAsync(app.Url, executable, cancellationToken);

            marker.Write(version);

            if (app.Run)
                Start(executable, app.Archive);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error($"Could not install silent app {app.Name}", ex);
        }
    }

    private static void Start(string executable, bool fromArchive)
    {
        if (!File.Exists(executable))
            return;

        string? directory = Path.GetDirectoryName(executable);
        Process? process = fromArchive
            ? ProcessRunner.Launch(executable, workingDirectory: directory)
            : ProcessRunner.StartHidden(executable, directory);
        process?.Dispose();
    }

    private static bool IsExtension(SilentAppEntry app, string extension) =>
        string.Equals(app.FileExtension, extension, StringComparison.OrdinalIgnoreCase);
}
