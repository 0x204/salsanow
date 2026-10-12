namespace SalsaNOW.Core.IO;

public static class PathRules
{
    public static bool IsInside(string path, string directory)
    {
        string prefix = directory.TrimEnd('\\', '/') + "\\";
        return Normalize(path).StartsWith(Normalize(prefix), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSafeToWipe(string directory)
    {
        string trimmed = Normalize(directory).TrimEnd('\\');
        if (trimmed.Length == 0 || trimmed.EndsWith(":", StringComparison.Ordinal) || trimmed.IndexOf('\\') <= 0)
            return false;

        return LastSegment(trimmed).IndexOf("Salsa", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string LastSegment(string path)
    {
        string trimmed = path.TrimEnd('\\', '/');
        int separator = trimmed.LastIndexOfAny(['\\', '/']);
        return separator >= 0 ? trimmed.Substring(separator + 1) : trimmed;
    }

    private static string Normalize(string path) => path.Replace('/', '\\');
}
