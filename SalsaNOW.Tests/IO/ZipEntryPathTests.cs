using SalsaNOW.Core.IO;

namespace SalsaNOW.Tests.IO;

public class ZipEntryPathTests
{
    private const string Root = @"C:\Users\kiosk\AppData\Local\Microsoft\dotnet";

    [Theory]
    [InlineData("dotnet.exe", 0, "dotnet.exe", false)]
    [InlineData("shared/Microsoft.NETCore.App/8.0.11/System.dll", 0, @"shared\Microsoft.NETCore.App\8.0.11\System.dll", false)]
    [InlineData("host/fxr/", 0, @"host\fxr", true)]
    [InlineData("/packs/readme.txt", 0, @"packs\readme.txt", false)]
    [InlineData("swidtag", 0x10, "swidtag", true)]
    public void TryResolve_MapsEntriesBelowTheRoot(string entry, int attributes, string expectedPath, bool expectedDirectory)
    {
        Assert.True(ZipEntryPath.TryResolve(Root, entry, attributes, out string relativePath, out bool isDirectory));
        Assert.Equal(expectedPath, relativePath);
        Assert.Equal(expectedDirectory, isDirectory);
    }

    [Theory]
    [InlineData("../evil.dll")]
    [InlineData("shared/../../evil.dll")]
    [InlineData(@"..\evil.dll")]
    [InlineData("C:/Windows/evil.dll")]
    [InlineData("")]
    [InlineData("/")]
    public void TryResolve_RejectsEntriesOutsideTheRoot(string entry)
    {
        Assert.False(ZipEntryPath.TryResolve(Root, entry, 0, out _, out _));
    }
}
