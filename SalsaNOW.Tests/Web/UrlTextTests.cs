using SalsaNOW.Core.Web;

namespace SalsaNOW.Tests.Web;

public class UrlTextTests
{
    [Theory]
    [InlineData("\"C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe\" --single-argument https://store.steampowered.com/login?code=abc",
        "https://store.steampowered.com/login?code=abc")]
    [InlineData("msedge.exe --profile-directory=Default \"https://example.com/a b\"", "https://example.com/a")]
    [InlineData("msedge.exe https://www.microsoft.com/edge/welcome https://example.org", "https://example.org")]
    public void FindNavigationUrl_ReturnsTheLinkToOpen(string commandLine, string expected)
    {
        Assert.Equal(expected, UrlText.FindNavigationUrl(commandLine));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("msedge.exe --type=renderer --lang=en-US")]
    [InlineData("msedge.exe --no-startup-window edge://settings https://go.microsoft.com/fwlink")]
    public void FindNavigationUrl_IgnoresCommandLinesWithoutUserLinks(string? commandLine)
    {
        Assert.Null(UrlText.FindNavigationUrl(commandLine));
    }

    [Fact]
    public void HostOf_DropsPathAndQuery()
    {
        Assert.Equal("accounts.example.com", UrlText.HostOf("https://accounts.example.com/oauth?token=secret"));
        Assert.Equal("an unparsable address", UrlText.HostOf("not a url"));
    }

    [Fact]
    public void Describe_KeepsHostAndPathOnly()
    {
        Assert.Equal("salsanowfiles.work/jsons/appsV2.json", UrlText.Describe("https://salsanowfiles.work/jsons/appsV2.json?x=1"));
    }

    [Fact]
    public void LaunchThrottle_SuppressesRepeatsWithinTheCooldown()
    {
        var throttle = new LaunchThrottle(TimeSpan.FromSeconds(3));
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.True(throttle.TryBegin("https://example.com", start));
        Assert.False(throttle.TryBegin("https://EXAMPLE.com", start.AddSeconds(2)));
        Assert.True(throttle.TryBegin("https://other.example", start.AddSeconds(2)));
        Assert.True(throttle.TryBegin("https://example.com", start.AddSeconds(3)));
    }
}
