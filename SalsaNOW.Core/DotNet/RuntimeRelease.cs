namespace SalsaNOW.Core.DotNet;

public static class RuntimeRelease
{
    public static string? ParseLatestVersion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return text!
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0 && char.IsDigit(line[0]));
    }

    public static bool IsChannelFolder(string folderName, string channel)
    {
        return folderName.StartsWith(channel + ".", StringComparison.OrdinalIgnoreCase)
            || string.Equals(folderName, channel, StringComparison.OrdinalIgnoreCase);
    }
}
