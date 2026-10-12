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

    private static readonly string SavedGames = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games");

    public async Task SetUpAsync(CancellationToken cancellationToken)
    {
        Log.Info("Setting up Cloud Save directory junctions...");

        List<string> folders = await LoadFoldersAsync(cancellationToken);
        folders.Add(SavedGames);
        Directory.CreateDirectory(paths.GameSaves);

        IReadOnlyList<GameSaveLink> links = GameSaveLinks.Plan(folders, paths.GameSaves, out IReadOnlyList<string> conflicts);
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

    private async Task<List<string>> LoadFoldersAsync(CancellationToken cancellationToken)
    {
        try
        {
            GameSavePathList list = await config.LoadAsync<GameSavePathList>(Endpoints.GameSavePaths, cancellationToken);
            return (list.Paths ?? []).Select(Normalize).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("Could not load the game save folder list", ex);
            return [];
        }
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

            if (KeepsExistingFiles(link.Folder) && !TryCopyExistingFiles(link))
                return false;

            Directory.CreateDirectory(Path.GetDirectoryName(link.Folder)!);
            if (Junction.TryReplaceDirectory(link.Folder, link.Target))
                return true;

            if (link.Folder.StartsWith(PublicDocuments, StringComparison.OrdinalIgnoreCase))
            {
                CloseNvidiaContainerWindows();
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    await Task.Delay(150, cancellationToken);
                    if (Junction.TryReplaceDirectory(link.Folder, link.Target))
                        return true;
                }
            }

            Log.Error($"Could not redirect {link.Folder} to Game Saves.");
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or TimeoutException or InvalidOperationException)
        {
            Log.Error($"Game Saves Setup Error for {link.Folder}", ex);
            return false;
        }
    }

    private static bool KeepsExistingFiles(string folder) =>
        string.Equals(folder, SteamPaths.Games, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(folder, SavedGames, StringComparison.OrdinalIgnoreCase);

    private static bool TryCopyExistingFiles(GameSaveLink link)
    {
        if (!Directory.Exists(link.Folder) || FileSystem.IsLink(link.Folder))
            return true;

        try
        {
            FileSystem.CopyMissingFiles(link.Folder, link.Target);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Could not copy the existing files of {link.Folder}", ex);
            return false;
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
