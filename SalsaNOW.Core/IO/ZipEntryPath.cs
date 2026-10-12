namespace SalsaNOW.Core.IO;

public static class ZipEntryPath
{
    private const int DirectoryAttribute = 0x10;

    public static bool TryResolve(string root, string entryFullName, int externalAttributes, out string relativePath, out bool isDirectory)
    {
        string relative = entryFullName.Replace('/', '\\').TrimStart('\\');
        isDirectory = relative.EndsWith("\\", StringComparison.Ordinal) || (externalAttributes & DirectoryAttribute) != 0;
        relativePath = relative.TrimEnd('\\');

        if (relativePath.Length == 0)
            return false;

        string[] segments = relativePath.Split('\\');
        if (segments.Any(segment => segment == ".." || segment.IndexOf(':') >= 0))
            return false;

        return PathRules.IsInside(root.TrimEnd('\\') + "\\" + relativePath, root);
    }
}
