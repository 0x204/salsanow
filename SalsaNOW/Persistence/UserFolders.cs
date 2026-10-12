using System.ComponentModel;
using System.Security;
using Microsoft.Win32;
using SalsaNOW.Configuration;
using SalsaNOW.Infrastructure;
using static SalsaNOW.Infrastructure.NativeMethods;

namespace SalsaNOW.Persistence;

internal static class UserFolders
{
    private static readonly string[] Libraries = ["Downloads", "Pictures", "Videos", "Music"];
    private static readonly TimeSpan TempRetention = TimeSpan.FromDays(1);

    public static void MoveToPersistentDrive(SalsaPaths paths)
    {
        string drive = Path.GetPathRoot(paths.Root)!;
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.Equals(Path.GetPathRoot(profile), drive, StringComparison.OrdinalIgnoreCase))
        {
            Log.Warn($"SalsaNOW is on {drive}, the same drive as the user profile. User folders stay where they are.");
            return;
        }

        Log.Info($"Moving user folders to {drive}...");
        foreach (string library in Libraries)
            Redirect(Path.Combine(profile, library), Path.Combine(drive, library));

        MoveTemp(Path.Combine(drive, "Temp"));
    }

    private static void Redirect(string folder, string target)
    {
        try
        {
            Directory.CreateDirectory(target);
            if (Directory.Exists(folder) && !FileSystem.IsLink(folder))
                FileSystem.CopyMissingFiles(folder, target);

            if (Junction.TryReplaceDirectory(folder, target))
                Log.Info($"{folder} now points to {target}.");
            else
                Log.Error($"Could not redirect {folder} to {target}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or TimeoutException or InvalidOperationException)
        {
            Log.Error($"Could not redirect {folder} to {target}", ex);
        }
    }

    private static void MoveTemp(string temp)
    {
        try
        {
            Directory.CreateDirectory(temp);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Environment") ?? throw new IOException(@"Could not open HKCU\Environment."))
            {
                key.SetValue("TEMP", temp, RegistryValueKind.ExpandString);
                key.SetValue("TMP", temp, RegistryValueKind.ExpandString);
            }

            Environment.SetEnvironmentVariable("TEMP", temp);
            Environment.SetEnvironmentVariable("TMP", temp);
            Log.Info($"Temporary files now go to {temp}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            Log.Error("Could not move the temporary folder", ex);
            return;
        }

        BackgroundJobs.Start("Temp folder setup", () =>
        {
            WindowTools.BroadcastSettingChange("Environment", timeoutMilliseconds: 5000);
            RemoveOldTempFiles(temp);
        });
    }

    private static void RemoveOldTempFiles(string temp)
    {
        DateTime bootTime = DateTime.UtcNow - TimeSpan.FromMilliseconds(GetTickCount64());
        DateTime retentionStart = DateTime.UtcNow - TempRetention;
        DateTime cutoff = bootTime > retentionStart ? bootTime : retentionStart;

        int removed = 0;
        int failed = 0;
        foreach (FileSystemInfo entry in new DirectoryInfo(temp).EnumerateFileSystemInfos())
        {
            try
            {
                if (!IsOlderThan(entry, cutoff))
                    continue;

                if (entry is DirectoryInfo)
                    FileSystem.DeleteDirectory(entry.FullName);
                else
                    FileSystem.DeleteFile(entry.FullName);

                removed++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failed++;
            }
        }

        if (removed > 0 || failed > 0)
            Log.Info($"Cleaned {temp}: removed {removed} old item(s), {failed} still in use.");
    }

    private static bool IsOlderThan(FileSystemInfo entry, DateTime cutoff)
    {
        if (entry.LastWriteTimeUtc >= cutoff)
            return false;

        if (entry is not DirectoryInfo directory || (directory.Attributes & FileAttributes.ReparsePoint) != 0)
            return true;

        return directory.EnumerateFileSystemInfos().All(child => IsOlderThan(child, cutoff));
    }
}
