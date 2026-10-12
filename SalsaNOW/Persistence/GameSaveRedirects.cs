using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using SalsaNOW.Configuration;
using SalsaNOW.Core.Persistence;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;
using SalsaNOW.Steam;
using static SalsaNOW.Infrastructure.NativeMethods;

namespace SalsaNOW.Persistence;

internal sealed class GameSaveRedirects(SalsaPaths paths, RemoteConfig config)
{
    private const string PublicDocuments = @"C:\Users\Public\Documents";

    public async Task SetUpAsync(CancellationToken cancellationToken)
    {
        Log.Info("Setting up Cloud Save directory junctions...");

        GameSavePathList list = await config.LoadAsync<GameSavePathList>(Endpoints.GameSavePaths, cancellationToken);
        Directory.CreateDirectory(paths.GameSaves);

        IReadOnlyList<GameSaveLink> links = GameSaveLinks.Plan(list.Paths.Select(Normalize), paths.GameSaves, out IReadOnlyList<string> conflicts);
        foreach (string conflict in conflicts)
            Log.Warn($"Skipping game save folder {conflict}: its name is already used by another one.");

        int failed = 0;
        foreach (GameSaveLink link in links)
        {
            if (!await RedirectAsync(link, cancellationToken))
                failed++;
        }

        if (failed == 0)
            Log.Info("Cloud Save junctions successfully created.");
        else
            Log.Warn($"Cloud Save junctions created with {failed} failure(s).");
    }

    private static string Normalize(string folder)
    {
        try
        {
            return Path.GetFullPath(folder).TrimEnd('\\');
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Log.Warn($"Skipping invalid game save folder \"{folder}\".");
            return "";
        }
    }

    private static async Task<bool> RedirectAsync(GameSaveLink link, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(link.Target);

            if (string.Equals(link.Folder, SteamPaths.Games, StringComparison.OrdinalIgnoreCase))
                CopyMissingFiles(link.Folder, link.Target);

            Directory.CreateDirectory(Path.GetDirectoryName(link.Folder)!);
            if (TryCreateJunction(link))
                return true;

            if (link.Folder.StartsWith(PublicDocuments, StringComparison.OrdinalIgnoreCase))
            {
                CloseNvidiaContainerWindows();
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    await Task.Delay(150, cancellationToken);
                    if (TryCreateJunction(link))
                        return true;
                }
            }

            Log.Error($"Could not redirect {link.Folder} to Game Saves.");
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or TimeoutException)
        {
            Log.Error($"Game Saves Setup Error for {link.Folder}", ex);
            return false;
        }
    }

    private static bool TryCreateJunction(GameSaveLink link)
    {
        try
        {
            FileSystem.DeleteDirectory(link.Folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        return ProcessRunner.Run("cmd.exe", $"/c mklink /J \"{link.Folder}\" \"{link.Target}\"", TimeSpan.FromSeconds(30)) == 0;
    }

    private static void CopyMissingFiles(string source, string destination)
    {
        if (!Directory.Exists(source) || FileSystem.IsLink(source))
            return;

        try
        {
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, file.Substring(source.Length).TrimStart('\\'));
                if (File.Exists(target))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }

            Log.Info(@"Persisted existing Steam steam\games folder contents.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error(@"Failed to persist Steam steam\games contents", ex);
        }
    }

    private static void CloseNvidiaContainerWindows()
    {
        var containerIds = new HashSet<uint>();
        foreach (Process process in Process.GetProcessesByName("NVDisplay.Container"))
        {
            using (process)
                containerIds.Add((uint)process.Id);
        }

        if (containerIds.Count == 0)
            return;

        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out uint processId);
            if (containerIds.Contains(processId))
            {
                var className = new StringBuilder(256);
                GetClassName(window, className, className.Capacity);
                if (className.ToString().StartsWith("NvContainerWindowClass", StringComparison.OrdinalIgnoreCase))
                    PostMessage(window, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }

            return true;
        }, IntPtr.Zero);
    }
}
