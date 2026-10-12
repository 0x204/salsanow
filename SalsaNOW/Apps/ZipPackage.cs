using System.IO.Compression;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;

namespace SalsaNOW.Apps;

internal static class ZipPackage
{
    public static async Task InstallAsync(string url, string targetDirectory, CancellationToken cancellationToken)
    {
        string archive = targetDirectory + ".zip";
        string staging = targetDirectory + ".new";
        string previous = targetDirectory + ".old";
        try
        {
            await Http.DownloadFileAsync(url, archive, cancellationToken);

            await FileSystem.DeleteDirectoryAsync(staging);
            ZipFile.ExtractToDirectory(archive, staging);

            await FileSystem.DeleteDirectoryAsync(previous);
            await SwapInAsync(staging, targetDirectory, previous);
        }
        finally
        {
            FileSystem.TryDeleteFile(archive);
            await TryDeleteDirectoryAsync(staging);
            if (Directory.Exists(targetDirectory))
                await TryDeleteDirectoryAsync(previous);
        }
    }

    private static async Task SwapInAsync(string staging, string target, string previous)
    {
        bool replacing = Directory.Exists(target);
        if (replacing)
            await FileSystem.MoveDirectoryAsync(target, previous);

        try
        {
            await FileSystem.MoveDirectoryAsync(staging, target);
        }
        catch when (replacing)
        {
            await FileSystem.MoveDirectoryAsync(previous, target);
            throw;
        }
    }

    private static async Task TryDeleteDirectoryAsync(string path)
    {
        try
        {
            await FileSystem.DeleteDirectoryAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Could not remove {path}: {ex.Message}");
        }
    }
}
