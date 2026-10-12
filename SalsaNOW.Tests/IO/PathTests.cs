using SalsaNOW.Core.IO;

namespace SalsaNOW.Tests.IO;

public class PathTests
{
    [Theory]
    [InlineData(@"I:\Apps\SalsaNOW\SalsaNOW.exe", @"I:\Apps\SalsaNOW", true)]
    [InlineData(@"I:\Apps\SalsaNOW\bin\SalsaNOW.exe", @"I:\Apps\SalsaNOW\", true)]
    [InlineData(@"i:\apps\salsanow\x", @"I:\Apps\SalsaNOW", true)]
    [InlineData(@"I:\Apps\SalsaNOW", @"I:\Apps\SalsaNOW", false)]
    [InlineData(@"I:\Apps\SalsaNOW2\x", @"I:\Apps\SalsaNOW", false)]
    [InlineData(@"C:\Users\kiosk\x", @"I:\Apps\SalsaNOW", false)]
    public void IsInside(string path, string directory, bool expected)
    {
        Assert.Equal(expected, PathRules.IsInside(path, directory));
    }

    [Theory]
    [InlineData(@"I:\Apps\SalsaNOW", true)]
    [InlineData(@"I:\Apps\SalsaNOW\", true)]
    [InlineData(@"I:\SalsaNOW", true)]
    [InlineData(@"I:\", false)]
    [InlineData(@"I:", false)]
    [InlineData(@"C:\Windows", false)]
    [InlineData(@"I:\Apps", false)]
    [InlineData("SalsaNOW", false)]
    [InlineData("", false)]
    public void IsSafeToWipe(string directory, bool expected)
    {
        Assert.Equal(expected, PathRules.IsSafeToWipe(directory));
    }

    [Theory]
    [InlineData(@"C:\Users\Public\Documents", "Documents")]
    [InlineData(@"C:\Users\Public\Documents\", "Documents")]
    [InlineData("C:/Program Files (x86)/Steam/steam/games", "games")]
    [InlineData("file.txt", "file.txt")]
    public void LastSegment(string path, string expected)
    {
        Assert.Equal(expected, PathRules.LastSegment(path));
    }

    [Theory]
    [InlineData(null, @"C:\dotnet", @"C:\dotnet")]
    [InlineData("", @"C:\dotnet", @"C:\dotnet")]
    [InlineData(@"C:\a;C:\b", @"C:\dotnet", @"C:\a;C:\b;C:\dotnet")]
    [InlineData(@"C:\a;", @"C:\dotnet", @"C:\a;C:\dotnet")]
    [InlineData(@"C:\a;c:\DOTNET\;C:\b", @"C:\dotnet", @"C:\a;c:\DOTNET\;C:\b")]
    [InlineData(@"%USERPROFILE%\bin", @"C:\dotnet", @"%USERPROFILE%\bin;C:\dotnet")]
    public void PathList_AddsMissingDirectoriesOnce(string? list, string directory, string expected)
    {
        Assert.Equal(expected, PathList.AddIfMissing(list, directory));
    }
}
