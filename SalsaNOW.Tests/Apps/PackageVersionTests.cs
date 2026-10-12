using SalsaNOW.Core.Apps;

namespace SalsaNOW.Tests.Apps;

public class PackageVersionTests
{
    [Theory]
    [InlineData(null, "1.0.0")]
    [InlineData("", "1.0.0")]
    [InlineData("   ", "1.0.0")]
    [InlineData(" 1.2.3 ", "1.2.3")]
    public void Normalize_FallsBackToDefault(string? input, string expected)
    {
        Assert.Equal(expected, PackageVersion.Normalize(input));
    }

    [Theory]
    [InlineData("1.2", "1.2.0.0")]
    [InlineData("1.2.3", "1.2.3.0")]
    [InlineData("2026.09.03", "2026.9.3.0")]
    [InlineData("EZManifest-1.4.0.zip", "1.4.0.0")]
    [InlineData("v1.0.1.1-beta", "1.0.1.1")]
    public void TryParse_FindsDottedVersion(string text, string expected)
    {
        Assert.True(PackageVersion.TryParse(text, out Version version));
        Assert.Equal(Version.Parse(expected), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("7")]
    public void TryParse_RejectsTextWithoutVersion(string? text)
    {
        Assert.False(PackageVersion.TryParse(text, out _));
    }

    [Theory]
    [InlineData("1.0.0", null, false)]
    [InlineData("1.0.0", "0.9", false)]
    [InlineData("1.0.1", null, true)]
    [InlineData("1.0.1", "garbage", true)]
    [InlineData("1.0.1", "1.0.0", true)]
    [InlineData("1.0.1", "1.0.1", false)]
    [InlineData("1.0.1", " 1.0.1\r\n", false)]
    [InlineData("1.0.1", "1.0.2", false)]
    [InlineData("2026.09.03", "1.0.0", true)]
    [InlineData("not a version", null, false)]
    public void IsUpdateAvailable(string remote, string? marker, bool expected)
    {
        Assert.Equal(expected, PackageVersion.IsUpdateAvailable(remote, marker));
    }
}
