using SalsaNOW.Apps;
using SalsaNOW.Configuration;
using SalsaNOW.Core.Apps;
using SalsaNOW.Core.IO;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;
using SalsaNOW.Shortcuts;
using SalsaNOW.Steam;

namespace SalsaNOW.Recovery;

internal static class RecoveryMenu
{
    private sealed record Option(string Text, Func<Task> Run);

    public static async Task RunAsync()
    {
        Option[] options =
        [
            new("Factory reset (This will remove the entire SalsaNOW folder and perform a fresh installation. Everything else on the I: drive will remain untouched.)", FactoryResetAsync),
            new("Restore default shortcuts", RestoreShortcutsAsync),
            new("Install Explorer++", InstallExplorerPlusPlusAsync),
            new("Install System Informer", InstallSystemInformerAsync),
            new("Exit", () =>
            {
                Environment.Exit(0);
                return Task.CompletedTask;
            }),
        ];

        FitConsole(options.Max(option => option.Text.Length) + 6);

        int selected = 0;
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Select an option (use arrows to navigate):\n");

            for (int i = 0; i < options.Length; i++)
            {
                if (i == selected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"> {options[i].Text}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  {options[i].Text}");
                }
            }

            switch (Console.ReadKey(intercept: true).Key)
            {
                case ConsoleKey.UpArrow:
                    selected = (selected - 1 + options.Length) % options.Length;
                    break;
                case ConsoleKey.DownArrow:
                    selected = (selected + 1) % options.Length;
                    break;
                case ConsoleKey.Enter:
                    Console.Clear();
                    await options[selected].Run();
                    break;
            }
        }
    }

    private static async Task FactoryResetAsync()
    {
        Console.WriteLine("Factory Reset selected.");

        SalsaPaths? paths = await ResolvePathsAsync();
        if (paths is null)
            return;

        if (!PathRules.IsSafeToWipe(paths.Root))
        {
            Pause($"Refusing to delete '{paths.Root}': it does not look like a SalsaNOW folder.");
            return;
        }

        Console.WriteLine($"Everything in {paths.Root} will be permanently deleted.");
        Console.Write("Press Y to continue or any other key to go back: ");
        if (Console.ReadKey(intercept: true).Key != ConsoleKey.Y)
            return;

        Console.WriteLine();
        Console.WriteLine("Deleting...");

        string executable = AppInfo.ExecutablePath;
        try
        {
            FileSystem.DeleteDirectoryExcept(paths.Root, executable);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Pause("Factory reset failed: " + ex.Message, seconds: 5);
            return;
        }

        Restart(executable);
    }

    private static async Task RestoreShortcutsAsync()
    {
        Console.WriteLine("Restore shortcuts selected.");

        SalsaPaths? paths = await ResolvePathsAsync();
        if (paths is null)
            return;

        try
        {
            List<AppEntry> apps = await AppCatalog.LoadAsync(new RemoteConfig(paths.ConfigCache), customAppsJson: null, CancellationToken.None);
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            foreach (AppEntry app in apps)
            {
                string target = app.IsArchive
                    ? Path.Combine(paths.Root, app.Name, app.ExeName)
                    : Path.Combine(paths.Root, app.ExeName);
                RestoreShortcut(paths, Path.Combine(desktop, app.Name + ".lnk"), target);
            }

            RestoreShortcut(paths, Path.Combine(desktop, "Steam.lnk"), SteamPaths.Executable);
        }
        catch (Exception ex)
        {
            Pause("Restoring shortcuts failed: " + ex.Message, seconds: 5);
            return;
        }

        Restart(AppInfo.ExecutablePath);
    }

    private static void RestoreShortcut(SalsaPaths paths, string shortcut, string target)
    {
        string fileName = Path.GetFileName(shortcut);
        try
        {
            ShortcutSync.Forget(paths, fileName);
            FileSystem.TryDeleteFile(shortcut);

            if (!File.Exists(target))
                return;

            ShellLink.Create(shortcut, target, Path.GetDirectoryName(target)!, keepCustomizations: false);
            if (!File.Exists(shortcut))
                return;

            ShortcutSync.Remember(paths, shortcut);

            string pinnedDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenShell", "Pinned");
            string pinned = Path.Combine(pinnedDirectory, fileName);
            Directory.CreateDirectory(pinnedDirectory);
            FileSystem.TryDeleteFile(pinned);
            File.Copy(shortcut, pinned, overwrite: true);

            Log.Info("Restored default shortcut: " + fileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("Could not restore " + fileName, ex);
        }
    }

    private static async Task InstallExplorerPlusPlusAsync()
    {
        Console.WriteLine("Installing Explorer++...");
        try
        {
            string destination = Path.Combine(Path.GetDirectoryName(AppInfo.ExecutablePath)!, "Explorer++.exe");
            await Http.DownloadFileAsync(Endpoints.ExplorerPlusPlus, destination);
            ProcessRunner.Launch(destination)?.Dispose();
            Log.Info("Explorer++ downloaded successfully.");
        }
        catch (Exception ex)
        {
            Pause("Installing Explorer++ failed: " + ex.Message);
        }
    }

    private static async Task InstallSystemInformerAsync()
    {
        Console.WriteLine("Installing System Informer...");
        try
        {
            string directory = Path.Combine(Path.GetDirectoryName(AppInfo.ExecutablePath)!, "System Informer");
            await ZipPackage.InstallAsync(Endpoints.SystemInformer, directory, CancellationToken.None);
            ProcessRunner.Launch(Path.Combine(directory, "SystemInformer.exe"))?.Dispose();
        }
        catch (Exception ex)
        {
            Pause("Installing System Informer failed: " + ex.Message);
        }
    }

    private static async Task<SalsaPaths?> ResolvePathsAsync()
    {
        SalsaPaths paths;
        try
        {
            paths = await SalsaPaths.DownloadAsync();
        }
        catch (Exception ex)
        {
            paths = new SalsaPaths(SalsaPaths.DefaultRoot);
            Log.Warn($"Could not load directory.json ({ex.Message}). Using {paths.Root}.");
        }

        if (!Directory.Exists(paths.Root))
        {
            Pause($"Global directory '{paths.Root}' does not exist, make sure you first install SalsaNOW.");
            return null;
        }

        return paths;
    }

    private static void Restart(string executable)
    {
        Console.Clear();
        ProcessRunner.Launch("cmd.exe", $"/c start \"\" \"{executable}\"")?.Dispose();
        Environment.Exit(0);
    }

    private static void Pause(string error, int seconds = 3)
    {
        Log.Error(error);
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
    }

    private static void FitConsole(int width)
    {
        try
        {
            int requiredWidth = Math.Min(width, Console.LargestWindowWidth);
            int requiredHeight = Math.Min(20, Console.LargestWindowHeight);

            if (Console.BufferWidth < requiredWidth)
                Console.SetBufferSize(requiredWidth, Console.BufferHeight);

            Console.SetWindowSize(requiredWidth, requiredHeight);
        }
        catch (Exception ex) when (ex is IOException or ArgumentOutOfRangeException or PlatformNotSupportedException)
        {
        }
    }
}
