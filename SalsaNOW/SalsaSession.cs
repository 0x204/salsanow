using SalsaNOW.Apps;
using SalsaNOW.Configuration;
using SalsaNOW.Core.Settings;
using SalsaNOW.Desktop;
using SalsaNOW.Guards;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;
using SalsaNOW.Nvidia;
using SalsaNOW.Persistence;
using SalsaNOW.Runtimes;
using SalsaNOW.Shell;
using SalsaNOW.Shortcuts;
using SalsaNOW.Steam;

namespace SalsaNOW;

internal static class SalsaSession
{
    public static async Task RunAsync(CommandLineOptions options, CancellationToken cancellationToken)
    {
        SalsaPaths paths = await ResolvePathsAsync(cancellationToken);

        var settingsFile = new UserSettingsFile(paths.SettingsFile);
        UserSettings? settings = settingsFile.TryRead();
        LogSessionSettings(settings);

        if (settings?.PersistUserFolders != false)
            UserFolders.MoveToPersistentDrive(paths);

        var disabledSilentApps = new List<string>(settings?.DisabledSilentApps?.Where(name => !string.IsNullOrWhiteSpace(name)) ?? []);
        if (settings?.CtrlWinApp is { } ctrlWinApp && !string.IsNullOrWhiteSpace(ctrlWinApp))
        {
            CtrlWinHotkey.Start(ctrlWinApp);
            disabledSilentApps.Add(CtrlWinHotkey.DefaultApp);
        }

        var config = new RemoteConfig(paths.ConfigCache);

        BackgroundJobs.Start("Environment setup", () => EnvironmentVariables.Configure(paths));
        BackgroundJobs.Start("Registry persistence", () => new RegistryPersistence(paths, config).RunAsync(cancellationToken));
        BackgroundJobs.Start("Game saves setup", () => new GameSaveRedirects(paths, config).SetUpAsync(cancellationToken));
        BackgroundJobs.Start("Shortcut sync", () => new ShortcutSync(paths).RunAsync(cancellationToken));
        BackgroundJobs.Start("CustomExplorer watcher", () => ExplorerShell.CloseCustomExplorerAsync(cancellationToken));
        BackgroundJobs.Start("Easy Anti-Cheat watcher", () => AntiCheatWatcher.RunAsync(cancellationToken));
        BackgroundJobs.Start("Launch option guard", () => LaunchOptionsGuard.RunAsync(cancellationToken));
        BackgroundJobs.Start(".NET runtime install", () => DotNetRuntimeInstaller.InstallAsync(cancellationToken));
        BackgroundJobs.Start("NVIDIA power mode", NvidiaPowerMode.Apply);

        await Task.WhenAll(
            RunLoggedAsync("Desktop setup", () => new DesktopInstaller(paths, config, bingWallpaper: settings?.BingWallpaper == true).InstallAsync(cancellationToken)),
            RunLoggedAsync("App install", () => new AppInstaller(paths, config, options.CustomAppsJson).InstallAsync(cancellationToken)),
            RunLoggedAsync("Silent app install", () => new SilentAppInstaller(paths, config, disabledSilentApps).InstallAsync(cancellationToken)),
            RunLoggedAsync("Steam setup", () => new SteamBootstrap(paths, silentLaunch: settings?.SteamSilentLaunch == true, steamInput: settings?.SteamInput == true).RunAsync(cancellationToken)));

        await new DesktopFinalizer(paths, new ShellSettings(paths, settingsFile)).RunAsync(cancellationToken);

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private static async Task RunLoggedAsync(string name, Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error($"{name} failed", ex);
        }
    }

    private static async Task<SalsaPaths> ResolvePathsAsync(CancellationToken cancellationToken)
    {
        SalsaPaths paths;
        string? fallbackReason = null;
        try
        {
            paths = await SalsaPaths.DownloadAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            fallbackReason = ex.Message;
            paths = new SalsaPaths(SalsaPaths.DefaultRoot);
        }

        Directory.CreateDirectory(paths.Root);
        Log.Initialize(paths.Root);

        if (fallbackReason is not null)
            Log.Warn($"Could not load directory.json ({fallbackReason}). Using {paths.Root}.");

        Log.Info($"Main directory created {paths.Root}");
        return paths;
    }

    private static void LogSessionSettings(UserSettings? settings)
    {
        if (settings?.SteamSilentLaunch == true)
            Log.Info("Steam silent launch enabled from SalsaNOWSettings.json.");
        if (settings?.SteamInput == true)
            Log.Info("Steam Input left enabled from SalsaNOWSettings.json.");
        if (settings?.BingWallpaper == true)
            Log.Info("Bing wallpaper of the day enabled from SalsaNOWSettings.json.");
        if (settings?.PersistUserFolders == false)
            Log.Info("User folders left on the system drive from SalsaNOWSettings.json.");
    }
}
