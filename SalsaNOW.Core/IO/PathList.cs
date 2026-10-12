namespace SalsaNOW.Core.IO;

public static class PathList
{
    public static string AddIfMissing(string? list, string directory)
    {
        if (string.IsNullOrWhiteSpace(list))
            return directory;

        if (Contains(list!, directory))
            return list!;

        return list!.TrimEnd(';') + ";" + directory;
    }

    public static bool Contains(string list, string directory)
    {
        string wanted = directory.TrimEnd('\\');
        return list
            .Split([';'], StringSplitOptions.RemoveEmptyEntries)
            .Any(entry => string.Equals(entry.TrimEnd('\\'), wanted, StringComparison.OrdinalIgnoreCase));
    }
}
