using SalsaNOW.Core.Steam;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Steam;

internal static class LaunchOptionsGuard
{
    private const string ConfigFileName = "localconfig.vdf";
    private static readonly TimeSpan UserDataPollInterval = TimeSpan.FromSeconds(5);

    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!Directory.Exists(SteamPaths.UserData))
                await Task.Delay(UserDataPollInterval, cancellationToken);

            using var watcher = new FileSystemWatcher(SteamPaths.UserData, ConfigFileName)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            };

            watcher.Created += (_, e) => InspectInBackground(e.FullPath);
            watcher.Changed += (_, e) => InspectInBackground(e.FullPath);
            watcher.Renamed += (_, e) => InspectInBackground(e.FullPath);
            watcher.EnableRaisingEvents = true;

            InspectExistingFiles();

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Error($"Brick prevention task failed: {ex.Message}, MAKE SURE YOU DO NOT USE THE STEAM LAUNCH OPTIONS");
        }
    }

    private static void InspectExistingFiles()
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(SteamPaths.UserData, ConfigFileName, SearchOption.AllDirectories))
                InspectInBackground(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("Could not check the existing Steam launch options: " + ex.Message);
        }
    }

    private static void InspectInBackground(string path) =>
        BackgroundJobs.Start("Launch option check", () => InspectAsync(path));

    private static async Task InspectAsync(string path)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                string content;
                using (var reader = new StreamReader(path))
                    content = await reader.ReadToEndAsync();

                if (SteamConfigText.HasLaunchOptions(content))
                    StopSteam(path);

                return;
            }
            catch (IOException)
            {
                await Task.Delay(200);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }

    private static void StopSteam(string configFile)
    {
        WindowTools.ShowConsole();
        Log.Error("STEAM LAUNCH OPTIONS DETECTED. Session terminated.");
        ProcessRunner.KillAll("steam", TimeSpan.FromSeconds(5));

        try
        {
            FileSystem.DeleteFile(configFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("Could not delete " + configFile, ex);
        }
    }
}
