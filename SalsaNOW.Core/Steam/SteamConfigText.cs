using System.Text.RegularExpressions;

namespace SalsaNOW.Core.Steam;

public static class SteamConfigText
{
    private static readonly Regex LaunchOptionsPattern =
        new("\"LaunchOptions\"\\s+\"\\s*[^\"\\s]", RegexOptions.Compiled);

    private static readonly Regex SteamInputPattern =
        new("(\"(SteamController_[^\"]*Support)\")\\s+\"[01]\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool HasLaunchOptions(string vdf) => LaunchOptionsPattern.IsMatch(vdf);

    public static bool TrySetSteamInput(string vdf, bool enabled, out string updated)
    {
        updated = vdf;
        if (!SteamInputPattern.IsMatch(vdf))
            return false;

        string value = enabled ? "1" : "0";
        updated = SteamInputPattern.Replace(vdf, "$1\t\t\"" + value + "\"");
        return !string.Equals(updated, vdf, StringComparison.Ordinal);
    }
}
