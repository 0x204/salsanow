using SalsaNOW.Core.DotNet;

namespace SalsaNOW.Tests.DotNet;

public class RuntimeReleaseTests
{
    [Theory]
    [InlineData("8.0.11", "8.0.11")]
    [InlineData("abc123def\r\n8.0.11\r\n", "8.0.11")]
    [InlineData("  10.0.0 \n", "10.0.0")]
    [InlineData("", null)]
    [InlineData("no version here", null)]
    public void ParseLatestVersion(string text, string? expected)
    {
        Assert.Equal(expected, RuntimeRelease.ParseLatestVersion(text));
    }

    [Theory]
    [InlineData("8.0.11", "8.0", true)]
    [InlineData("10.0.0-rc.1.25451.107", "10.0", true)]
    [InlineData("10.0.0", "1.0", false)]
    [InlineData("8.0.11", "6.0", false)]
    public void IsChannelFolder(string folder, string channel, bool expected)
    {
        Assert.Equal(expected, RuntimeRelease.IsChannelFolder(folder, channel));
    }
}
