using System.Text.RegularExpressions;

namespace SalsaNOW.Core.Apps;

public static class PackageVersion
{
    public const string Default = "1.0.0";

    private static readonly Version Baseline = new(1, 0, 0, 0);

    private static readonly Regex VersionPattern = new(@"\d+\.\d+(?:\.\d+){0,2}");

    public static string Normalize(string? version)
    {
        return string.IsNullOrWhiteSpace(version) ? Default : version!.Trim();
    }

    public static bool IsUpdateAvailable(string remoteVersion, string? installedMarker)
    {
        if (!TryParse(remoteVersion, out Version remote) || remote <= Baseline)
            return false;

        if (installedMarker is null || !TryParse(installedMarker.Trim(), out Version installed))
            return true;

        return remote > installed;
    }

    public static bool TryParse(string? text, out Version version)
    {
        version = Baseline;
        if (string.IsNullOrEmpty(text))
            return false;

        Match match = VersionPattern.Match(text);
        if (!match.Success || !Version.TryParse(match.Value, out Version? parsed))
            return false;

        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
        return true;
    }
}
