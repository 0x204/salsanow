using System.Text.RegularExpressions;

namespace SalsaNOW.Core.Web;

public static class UrlText
{
    private static readonly Regex UrlPattern = new(@"https?://[^\s'""]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EdgeNoisePattern = new(@"edge://|microsoft\.com", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? FindNavigationUrl(string? commandLine)
    {
        if (string.IsNullOrEmpty(commandLine))
            return null;

        return UrlPattern.Matches(commandLine)
            .Cast<Match>()
            .Select(match => match.Value)
            .FirstOrDefault(url => !EdgeNoisePattern.IsMatch(url));
    }

    public static string Describe(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ? uri.Host + uri.AbsolutePath : url;
    }

    public static string HostOf(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ? uri.Host : "an unparsable address";
    }
}
