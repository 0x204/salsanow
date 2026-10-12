using System.Text;
using SalsaNOW.Core.IO;

namespace SalsaNOW.Core.Shortcuts;

public static class ShortcutFile
{
    public const string BackupExtension = ".bak";

    public static readonly string[] SearchPatterns = ["*.lnk", "*.url"];

    private const uint ShellLinkHeaderSize = 0x4C;

    private static readonly byte[] ShellLinkClsid =
    [
        0x01, 0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46,
    ];

    public static string? LiveNameFromBackup(string path)
    {
        string name = PathRules.LastSegment(path);
        if (name.Length <= BackupExtension.Length || !name.EndsWith(BackupExtension, StringComparison.OrdinalIgnoreCase))
            return null;

        string shortcut = name.Substring(0, name.Length - BackupExtension.Length);
        return IsShortcutName(shortcut) ? shortcut : null;
    }

    public static bool IsValid(string fileName, byte[]? bytes)
    {
        if (bytes is null || bytes.Length < 20)
            return false;

        string name = fileName.EndsWith(BackupExtension, StringComparison.OrdinalIgnoreCase)
            ? fileName.Substring(0, fileName.Length - BackupExtension.Length)
            : fileName;

        if (name.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
            return Encoding.ASCII.GetString(bytes).IndexOf("URL=", StringComparison.OrdinalIgnoreCase) >= 0;

        if (!name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            return false;

        if (BitConverter.ToUInt32(bytes, 0) != ShellLinkHeaderSize)
            return false;

        for (int i = 0; i < ShellLinkClsid.Length; i++)
        {
            if (bytes[4 + i] != ShellLinkClsid[i])
                return false;
        }

        return true;
    }

    private static bool IsShortcutName(string name)
    {
        return name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".url", StringComparison.OrdinalIgnoreCase);
    }
}
