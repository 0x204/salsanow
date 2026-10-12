using SalsaNOW.Configuration;
using SalsaNOW.Core.Shortcuts;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Shortcuts;

internal sealed class ShortcutSync(SalsaPaths paths)
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly string _desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    private readonly string _startMenu = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @"Microsoft\Windows\Start Menu\Programs");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(paths.SavedShortcuts);
        Directory.CreateDirectory(paths.DeletedShortcuts);
        Directory.CreateDirectory(_startMenu);

        try
        {
            AddBackupExtension(paths.SavedShortcuts);
            AddBackupExtension(paths.DeletedShortcuts);
            RestoreSavedShortcuts();
            UnpackBackupsOnDesktop();
            Log.Info("Initial Desktop shortcut sync completed.");
        }
        catch (Exception ex)
        {
            Log.Error("Initial shortcut sync failed", ex);
        }

        while (true)
        {
            await Task.Delay(Interval, cancellationToken);

            try
            {
                UnpackBackupsOnDesktop();
                SaveDesktopChanges();
            }
            catch (Exception ex)
            {
                Log.Error("Shortcut sync failed", ex);
            }
        }
    }

    public static bool IsKnown(SalsaPaths paths, string fileName)
    {
        return KnownCopies(paths, fileName).Any(File.Exists);
    }

    public static void Forget(SalsaPaths paths, string fileName)
    {
        foreach (string copy in KnownCopies(paths, fileName))
            FileSystem.TryDeleteFile(copy);
    }

    public static void Remember(SalsaPaths paths, string desktopShortcut)
    {
        if (!File.Exists(desktopShortcut))
            return;

        string fileName = Path.GetFileName(desktopShortcut);
        Forget(paths, fileName);
        Directory.CreateDirectory(paths.SavedShortcuts);
        File.Copy(desktopShortcut, Path.Combine(paths.SavedShortcuts, fileName + ShortcutFile.BackupExtension), overwrite: true);
    }

    private static IEnumerable<string> KnownCopies(SalsaPaths paths, string fileName)
    {
        string backupName = fileName + ShortcutFile.BackupExtension;
        yield return Path.Combine(paths.SavedShortcuts, fileName);
        yield return Path.Combine(paths.SavedShortcuts, backupName);
        yield return Path.Combine(paths.DeletedShortcuts, fileName);
        yield return Path.Combine(paths.DeletedShortcuts, backupName);
    }

    private static void AddBackupExtension(string directory)
    {
        foreach (string file in ShortcutFile.SearchPatterns.SelectMany(pattern => Directory.GetFiles(directory, pattern)))
        {
            string backup = file + ShortcutFile.BackupExtension;
            if (!File.Exists(backup))
                File.Move(file, backup);
        }
    }

    private void RestoreSavedShortcuts()
    {
        foreach (string backup in Directory.GetFiles(paths.SavedShortcuts, "*" + ShortcutFile.BackupExtension))
        {
            string? name = ShortcutFile.LiveNameFromBackup(backup);
            byte[]? content = name is null ? null : ReadShortcut(backup);
            if (name is not null && content is not null)
                PlaceShortcut(Path.Combine(_desktop, name), content);
        }
    }

    private void UnpackBackupsOnDesktop()
    {
        if (!Directory.Exists(_desktop))
            return;

        foreach (string file in Directory.GetFiles(_desktop))
        {
            string? name = ShortcutFile.LiveNameFromBackup(file);
            if (name is null)
                continue;

            byte[]? content = ReadShortcut(file);
            if (content is null || !PlaceShortcut(Path.Combine(_desktop, name), content))
                continue;

            try
            {
                FileSystem.DeleteFile(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Error("Shortcut copy failed for " + Path.GetFileName(file), ex);
            }
        }
    }

    private void SaveDesktopChanges()
    {
        if (!Directory.Exists(_desktop))
            return;

        var onDesktop = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in ShortcutFile.SearchPatterns.SelectMany(pattern => Directory.GetFiles(_desktop, pattern)))
        {
            string name = Path.GetFileName(file);
            onDesktop.Add(name);

            string saved = Path.Combine(paths.SavedShortcuts, name + ShortcutFile.BackupExtension);
            byte[]? content = ReadShortcut(file);
            if (content is null)
            {
                byte[]? savedContent = ReadShortcut(saved);
                if (savedContent is not null)
                {
                    PlaceShortcut(file, savedContent);
                    Log.Info("Restored shortcut: " + name);
                }

                continue;
            }

            if (!HasContent(saved, content))
            {
                WriteShortcut(saved, content);
                Log.Info("Saved modified shortcut: " + name + ShortcutFile.BackupExtension);
            }

            PlaceShortcut(Path.Combine(_startMenu, name), content);
            FileSystem.DeleteFile(Path.Combine(paths.DeletedShortcuts, name + ShortcutFile.BackupExtension));
        }

        foreach (string saved in Directory.GetFiles(paths.SavedShortcuts, "*" + ShortcutFile.BackupExtension))
        {
            string? name = ShortcutFile.LiveNameFromBackup(saved);
            if (name is null || onDesktop.Contains(name))
                continue;

            string deleted = Path.Combine(paths.DeletedShortcuts, name + ShortcutFile.BackupExtension);
            File.SetAttributes(saved, FileAttributes.Normal);
            if (File.Exists(deleted))
            {
                File.Delete(saved);
            }
            else
            {
                File.Move(saved, deleted);
                Log.Info("Moved deleted shortcut to long-term backup: " + name + ShortcutFile.BackupExtension);
            }
        }
    }

    private static bool PlaceShortcut(string destination, byte[] content)
    {
        return HasContent(destination, content) || WriteShortcut(destination, content);
    }

    private static bool HasContent(string path, byte[] content)
    {
        byte[]? existing = ReadShortcut(path);
        return existing is not null && existing.SequenceEqual(content);
    }

    private static byte[]? ReadShortcut(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            byte[] content = File.ReadAllBytes(path);
            return ShortcutFile.IsValid(path, content) ? content : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool WriteShortcut(string destination, byte[] content)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            string temporary = destination + ".tmp";
            File.WriteAllBytes(temporary, content);
            FileSystem.ReplaceFile(temporary, destination);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("Shortcut copy failed for " + Path.GetFileName(destination), ex);
            return false;
        }
    }
}
