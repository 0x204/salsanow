using SalsaNOW.Core.Apps;

namespace SalsaNOW.Tests.Apps;

public class SilentAppEntryTests
{
    private static readonly SilentAppEntry MenuToggle = new()
    {
        Name = "Windows Menu Toggle CTRL WIN",
        FileName = "WindowsMenuToggle",
        FileExtension = "exe",
    };

    [Theory]
    [InlineData("WindowsMenuToggle")]
    [InlineData("windowsmenutoggle")]
    [InlineData("Windows Menu Toggle CTRL WIN")]
    [InlineData("WindowsMenuToggle.exe")]
    [InlineData("  WindowsMenuToggle ")]
    public void MatchesDisplayOrFileName(string name)
    {
        Assert.True(MenuToggle.IsNamed(name));
    }

    [Theory]
    [InlineData("WindowsMenuToggle.zip")]
    [InlineData("CtrlTab")]
    [InlineData("")]
    public void RejectsOtherNames(string name)
    {
        Assert.False(MenuToggle.IsNamed(name));
    }
}
