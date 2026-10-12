using System.ComponentModel;
using SalsaNOW.Configuration;
using SalsaNOW.Core.Persistence;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;

namespace SalsaNOW.Persistence;

internal sealed class RegistryPersistence(SalsaPaths paths, RemoteConfig config)
{
    private static readonly TimeSpan DownloadLimit = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromMinutes(2);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Task<RegistryBackupList> backupList = config.LoadAsync<RegistryBackupList>(Endpoints.RegistryBackupList, cancellationToken, DownloadLimit);
        await ImportRegistryFilesAsync(cancellationToken);

        RegistryBackupList backups;
        try
        {
            backups = await backupList;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Error("Could not load the registry backup list", ex);
            return;
        }

        Directory.CreateDirectory(paths.RegistryBackups);

        RestoreBackups(backups);
        await KeepBackingUpAsync(backups, cancellationToken);
    }

    private async Task ImportRegistryFilesAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(paths.RegistryFiles);

        List<RegistryFileEntry> files;
        try
        {
            files = await config.LoadAsync<List<RegistryFileEntry>>(Endpoints.RegistryFiles, cancellationToken, DownloadLimit);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Error("Custom registry files failed", ex);
            return;
        }

        using var downloads = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        downloads.CancelAfter(DownloadLimit);

        foreach (RegistryFileEntry entry in files)
        {
            string file = Path.Combine(paths.RegistryFiles, Path.GetFileName(entry.File));
            try
            {
                await Http.DownloadFileAsync(entry.Url, file, downloads.Token);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (!File.Exists(file))
                {
                    Log.Error($"Could not download {entry.File}", ex);
                    continue;
                }

                string reason = ex is OperationCanceledException ? "timed out" : ex.Message;
                Log.Warn($"Could not download {entry.File} ({reason}). Importing the copy from the last session.");
            }

            try
            {
                int exitCode = RunReg($"import \"{file}\"");
                if (exitCode != 0)
                    Log.Error($"Failed to import {entry.File} (reg.exe exit code {exitCode}).");
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                Log.Error($"Failed to import {entry.File}", ex);
            }
        }
    }

    private void RestoreBackups(RegistryBackupList backups)
    {
        foreach (RegistryBackupEntry entry in backups.RegistryKeys)
        {
            string file = BackupFile(entry);
            if (!File.Exists(file))
                continue;

            Log.Info("Restoring registry: " + entry.Key);
            string? failure;
            try
            {
                int exitCode = RunReg($"import \"{file}\"");
                failure = exitCode == 0 ? null : $"reg.exe exit code {exitCode}";
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                failure = ex.Message;
            }

            if (failure is not null)
                KeepFailedBackup(entry, file, failure);
        }

        Log.Info("Initial registry restore complete.");
    }

    private static void KeepFailedBackup(RegistryBackupEntry entry, string file, string reason)
    {
        try
        {
            File.Copy(file, file + ".failed", overwrite: true);
            Log.Error($"Failed to restore {entry.Key} ({reason}). The backup was kept as {Path.GetFileName(file)}.failed.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Failed to restore {entry.Key} ({reason}) and could not keep a copy of the backup", ex);
        }
    }

    private async Task KeepBackingUpAsync(RegistryBackupList backups, CancellationToken cancellationToken)
    {
        var failingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        TimeSpan interval = TimeSpan.FromSeconds(Math.Max(1, backups.BackupIntervalSeconds));

        while (true)
        {
            foreach (RegistryBackupEntry entry in backups.RegistryKeys)
                BackUp(entry, failingKeys);

            await Task.Delay(interval, cancellationToken);
        }
    }

    private void BackUp(RegistryBackupEntry entry, HashSet<string> failingKeys)
    {
        string file = BackupFile(entry);
        string export = file + ".tmp";

        try
        {
            int exitCode = RunReg($"export \"{entry.Key}\" \"{export}\" /y", ExportTimeout);
            if (exitCode != 0 || !File.Exists(export))
            {
                if (failingKeys.Add(entry.Key))
                    Log.Warn($"Could not back up {entry.Key} (reg.exe exit code {exitCode}).");
                return;
            }

            failingKeys.Remove(entry.Key);
            if (!FileSystem.ContentEquals(export, file))
                FileSystem.ReplaceFile(export, file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException or TimeoutException)
        {
            if (failingKeys.Add(entry.Key))
                Log.Error("Failed to back up " + entry.Key, ex);
        }
        finally
        {
            FileSystem.TryDeleteFile(export);
        }
    }

    private string BackupFile(RegistryBackupEntry entry) => Path.Combine(paths.RegistryBackups, Path.GetFileName(entry.File));

    private static int RunReg(string arguments, TimeSpan? timeout = null) => ProcessRunner.Run("reg.exe", arguments, timeout);
}
