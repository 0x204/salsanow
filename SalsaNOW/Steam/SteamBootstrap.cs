using System.Diagnostics;
using SalsaNOW.Configuration;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;

namespace SalsaNOW.Steam;

internal sealed class SteamBootstrap(SalsaPaths paths, bool silentLaunch, bool steamInput)
{
    private const string UiChunkName = "chunk~2dcc5aaf7.js";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            string bypass = paths.InRoot("conhost.exe");
            string chunk = paths.InRoot(UiChunkName);

            Task steamInputApplied = SteamInput.ApplyAsync(steamInput);
            Task bypassDownloaded = Http.DownloadFileAsync(Endpoints.SteamProxyBypass, bypass, cancellationToken);
            Task chunkDownloaded = Http.DownloadFileAsync(Endpoints.SteamUiChunk, chunk, cancellationToken);

            SwapSteamUi();
            await Task.WhenAll(steamInputApplied, bypassDownloaded, chunkDownloaded);

            string installedChunk = Path.Combine(SteamPaths.SteamUi, UiChunkName);
            FileSystem.DeleteFile(installedChunk);
            File.Move(chunk, installedChunk);

            Process? bypassProcess = ProcessRunner.Launch(bypass, silentLaunch ? "-silent" : "");
            WindowTools.HideConsole();

            await Task.Delay(200, cancellationToken);
            ClearAppCache();

            if (File.Exists(paths.StartupBatch))
                ProcessRunner.Launch(paths.StartupBatch)?.Dispose();

            BackgroundJobs.Start("Steam proxy cleanup", () => DeleteAfterExitAsync(bypassProcess, bypass));
            Log.Info("Steam Proxy successfully bypassed.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("Steam Proxy Shutdown Error", ex);
        }
    }

    private static void SwapSteamUi()
    {
        string original = SteamPaths.SteamUi + "NV";
        string copy = SteamPaths.SteamUi + "OG";
        if (Directory.Exists(original))
            return;

        int exitCode = ProcessRunner.Run("robocopy.exe", $"\"{SteamPaths.SteamUi}\" \"{copy}\" /E /COPY:DAT /R:0 /W:0 /MT:16 /NFL /NDL /NJH /NJS /NC /NS");

        if (exitCode >= 8)
            throw new IOException($"Copying steamui failed (robocopy exit code {exitCode}).");

        FileSystem.DeleteFile(Path.Combine(copy, UiChunkName));
        Directory.Move(SteamPaths.SteamUi, original);
        Directory.Move(copy, SteamPaths.SteamUi);
    }

    private static void ClearAppCache()
    {
        try
        {
            FileSystem.DeleteDirectory(SteamPaths.AppCache);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("Could not clear Steam's appcache: " + ex.Message);
        }
    }

    private static async Task DeleteAfterExitAsync(Process? process, string path)
    {
        using (process)
        {
            while (process is not null && !process.HasExited)
                await Task.Delay(1000);
        }

        await Task.Delay(200);
        FileSystem.TryDeleteFile(path);
    }
}
