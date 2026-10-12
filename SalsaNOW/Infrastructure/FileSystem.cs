using System.Text;
using Microsoft.Win32.SafeHandles;
using SalsaNOW.Core.IO;
using static SalsaNOW.Infrastructure.NativeMethods;

namespace SalsaNOW.Infrastructure;

internal static class FileSystem
{
    public static async Task DeleteDirectoryAsync(string path, int attempts = 3)
    {
        for (int attempt = 1; Directory.Exists(path); attempt++)
        {
            try
            {
                DeleteDirectory(path);
                return;
            }
            catch (Exception ex) when (attempt < attempts && ex is IOException or UnauthorizedAccessException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    public static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        if (IsLink(path))
        {
            Directory.Delete(path);
            return;
        }

        foreach (string file in Directory.GetFiles(path))
            DeleteFile(file);

        foreach (string subdirectory in Directory.GetDirectories(path))
            DeleteDirectory(subdirectory);

        var info = new DirectoryInfo(path);
        info.Attributes &= ~(FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System);
        info.Delete();
    }

    public static void DeleteDirectoryExcept(string directory, string keepFile)
    {
        foreach (string file in Directory.GetFiles(directory))
        {
            if (!string.Equals(file, keepFile, StringComparison.OrdinalIgnoreCase))
                DeleteFile(file);
        }

        foreach (string subdirectory in Directory.GetDirectories(directory))
        {
            if (!IsLink(subdirectory) && PathRules.IsInside(keepFile, subdirectory))
                DeleteDirectoryExcept(subdirectory, keepFile);
            else
                DeleteDirectory(subdirectory);
        }

        if (!PathRules.IsInside(keepFile, directory))
            DeleteDirectory(directory);
    }

    public static async Task MoveDirectoryAsync(string source, string destination, int attempts = 3)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                Directory.Move(source, destination);
                return;
            }
            catch (Exception ex) when (attempt < attempts && ex is IOException or UnauthorizedAccessException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    public static int CopyMissingFiles(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        int keptAside = 0;

        foreach (string file in Directory.GetFiles(source))
        {
            string target = Path.Combine(destination, Path.GetFileName(file));
            if (!File.Exists(target))
            {
                File.Copy(file, target);
            }
            else if (!ContentEquals(file, target))
            {
                File.Copy(file, FreeNumberedName(target));
                keptAside++;
            }
        }

        foreach (string subdirectory in Directory.GetDirectories(source))
        {
            if (!IsLink(subdirectory))
                keptAside += CopyMissingFiles(subdirectory, Path.Combine(destination, Path.GetFileName(subdirectory)));
        }

        return keptAside;
    }

    public static void DeleteFile(string path)
    {
        if (!File.Exists(path))
            return;

        File.SetAttributes(path, FileAttributes.Normal);
        File.Delete(path);
    }

    public static bool TryDeleteFile(string path)
    {
        try
        {
            DeleteFile(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void ReplaceFile(string source, string destination)
    {
        if (!File.Exists(destination))
        {
            File.Move(source, destination);
            return;
        }

        File.SetAttributes(destination, FileAttributes.Normal);
        try
        {
            File.Replace(source, destination, destinationBackupFileName: null);
        }
        catch (IOException)
        {
            File.Delete(destination);
            File.Move(source, destination);
        }
    }

    public static void WriteAllTextAtomic(string path, string contents)
    {
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, contents);
        ReplaceFile(temporary, path);
    }

    public static bool ContentEquals(string left, string right)
    {
        var leftInfo = new FileInfo(left);
        var rightInfo = new FileInfo(right);
        if (!leftInfo.Exists || !rightInfo.Exists || leftInfo.Length != rightInfo.Length)
            return false;

        const int BufferSize = 81920;
        using FileStream leftStream = leftInfo.OpenRead();
        using FileStream rightStream = rightInfo.OpenRead();
        var leftBuffer = new byte[BufferSize];
        var rightBuffer = new byte[BufferSize];

        int read;
        while ((read = ReadBlock(leftStream, leftBuffer)) > 0)
        {
            if (ReadBlock(rightStream, rightBuffer) != read)
                return false;

            for (int i = 0; i < read; i++)
            {
                if (leftBuffer[i] != rightBuffer[i])
                    return false;
            }
        }

        return true;
    }

    public static bool IsLink(string path)
    {
        return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    }

    public static bool IsSameDirectory(string left, string right)
    {
        string? leftPath = FinalPath(left);
        return leftPath is not null && string.Equals(leftPath, FinalPath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string? FinalPath(string directory)
    {
        using SafeFileHandle handle = CreateFile(directory, FILE_READ_ATTRIBUTES, FileShare.ReadWrite | FileShare.Delete, IntPtr.Zero, FileMode.Open, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        if (handle.IsInvalid)
            return null;

        var path = new StringBuilder(1024);
        uint length = GetFinalPathNameByHandle(handle, path, (uint)path.Capacity, 0);
        return length > 0 && length < path.Capacity ? path.ToString() : null;
    }

    private static string FreeNumberedName(string path)
    {
        string directory = Path.GetDirectoryName(path)!;
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);

        for (int number = 1; ; number++)
        {
            string candidate = Path.Combine(directory, $"{name} ({number}){extension}");
            if (!File.Exists(candidate))
                return candidate;
        }
    }

    private static int ReadBlock(Stream stream, byte[] buffer)
    {
        int total = 0;
        int read;
        while (total < buffer.Length && (read = stream.Read(buffer, total, buffer.Length - total)) > 0)
            total += read;

        return total;
    }
}
