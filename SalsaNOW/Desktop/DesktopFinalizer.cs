using SalsaNOW.Apps;
using SalsaNOW.Configuration;
using SalsaNOW.Infrastructure;
using SalsaNOW.Shell;
using SalsaNOW.Shortcuts;
using SalsaNOW.Steam;

namespace SalsaNOW.Desktop;

internal sealed class DesktopFinalizer(SalsaPaths paths, ShellSettings shellSettings)
{
    private static readonly string[] OpenShellPinnedShortcuts =
    [
        "7-Zip.lnk",
        "Brave.lnk",
        "Explorer++.lnk",
        "Notepad++.lnk",
        "PeaZip File Explorer Archiver.lnk",
        "Steam.lnk",
        "System Informer.lnk",
    ];

    private readonly string _desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Task startHookClosed = ExplorerShell.CloseStartHookWindowAsync(cancellationToken);

        Step("Removing DXCache", RemoveDxCache);
        Step("File associations", () => FileAssociations.ApplyDefaults(paths));
        Step("SalsaNOWSettings.json", () => shellSettings.Start(cancellationToken));
        Step("Desktop context menus", AddDesktopMenuCommands);
        Step("Steam shortcut", CreateSteamShortcut);
        Step("Open-Shell pins", PinOpenShellShortcuts);
        Step("Explorer++ reset", ResetExplorerPlusPlus);

        await Task.WhenAny(startHookClosed, Task.Delay(1500, cancellationToken));

        Step("Open-Shell", StartOpenShell);
        Step("Shell appearance", shellSettings.RefreshShellAppearance);
        Step("First-run notice", () => WelcomeNotice.ShowOnce(paths.WelcomeMarker));
    }

    private static void Step(string name, Action step)
    {
        try
        {
            step();
        }
        catch (Exception ex)
        {
            Log.Error($"{name} failed", ex);
        }
    }

    private void RemoveDxCache()
    {
        string dxCache = Path.Combine(paths.GameSaves, "DXCache");
        if (!Directory.Exists(dxCache))
            return;

        FileSystem.DeleteDirectory(dxCache);
        Log.Info(@"Removed Game Saves\DXCache.");
    }

    private void AddDesktopMenuCommands()
    {
        AddDesktopMenuCommand("ChangeWallpaper", "Change Wallpaper (USE THIS)", Path.Combine(paths.SilentApps, "SalsaChangeWallpaper.exe"));
        AddDesktopMenuCommand("RestartSteam", "Restart Steam", Path.Combine(paths.SilentApps, "RestartSteam.exe"));
        Log.Info("Desktop context menus applied.");
    }

    private static void AddDesktopMenuCommand(string keyName, string title, string executable)
    {
        if (!File.Exists(executable))
        {
            Log.Warn($"Skipping context menu {title}, missing: {executable}");
            return;
        }

        string keyPath = @"Software\Classes\Directory\Background\shell\" + keyName;
        UserRegistry.SetString(keyPath, "MUIVerb", title);
        UserRegistry.SetString(keyPath, "Position", "Bottom");
        UserRegistry.SetString(keyPath + @"\command", "", $"\"{executable}\"");
    }

    private void CreateSteamShortcut()
    {
        if (ShortcutSync.IsKnown(paths, "Steam.lnk"))
        {
            Log.Info("Steam shortcut already saved. Skipping desktop create.");
            return;
        }

        ShellLink.Create(Path.Combine(_desktop, "Steam.lnk"), SteamPaths.Executable, SteamPaths.Root);
        Log.Info("Steam desktop shortcut created.");
    }

    private void PinOpenShellShortcuts()
    {
        string pinned = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenShell", "Pinned");
        Directory.CreateDirectory(pinned);

        foreach (string shortcut in OpenShellPinnedShortcuts)
        {
            string source = Path.Combine(_desktop, shortcut);
            string destination = Path.Combine(pinned, shortcut);
            if (!File.Exists(source) || File.Exists(destination))
                continue;

            try
            {
                File.Copy(source, destination);
                Log.Info("Pinned Open-Shell shortcut: " + shortcut);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Error("Failed to pin " + shortcut, ex);
            }
        }
    }

    private static void ResetExplorerPlusPlus()
    {
        if (UserRegistry.DeleteTree(@"Software\Explorer++"))
            Log.Info("Removed Explorer++ registry key.");
    }

    private void StartOpenShell()
    {
        string startMenu = Path.Combine(paths.OpenShell, "StartMenu.exe");
        if (!File.Exists(startMenu))
        {
            Log.Warn("Open-Shell StartMenu.exe not found.");
            return;
        }

        string menuSettings = Path.Combine(paths.OpenShell, "Menu Settings.xml");
        if (File.Exists(menuSettings))
        {
            using var import = ProcessRunner.Launch(startMenu, $"-xml \"{menuSettings}\"");
            import?.WaitForExit(10000);
        }

        ProcessRunner.Launch(startMenu)?.Dispose();
        Log.Info("Open-Shell started.");
    }
}
