using System.Text;
using SalsaNOW.Core.Shortcuts;

namespace SalsaNOW.Tests.Shortcuts;

public class ShortcutFileTests
{
    [Theory]
    [InlineData(@"I:\Apps\SalsaNOW\Shortcuts\Steam.lnk.bak", "Steam.lnk")]
    [InlineData(@"C:\Users\kiosk\Desktop\Discord.url.BAK", "Discord.url")]
    [InlineData("Notepad++.lnk.bak", "Notepad++.lnk")]
    public void LiveNameFromBackup_StripsTheBackupExtension(string path, string expected)
    {
        Assert.Equal(expected, ShortcutFile.LiveNameFromBackup(path));
    }

    [Theory]
    [InlineData("Steam.lnk")]
    [InlineData("notes.txt.bak")]
    [InlineData(".bak")]
    [InlineData("Steam.lnk.bak.bak")]
    public void LiveNameFromBackup_IgnoresOtherFiles(string path)
    {
        Assert.Null(ShortcutFile.LiveNameFromBackup(path));
    }

    [Theory]
    [InlineData("Steam.lnk")]
    [InlineData("Steam.lnk.bak")]
    public void IsValid_AcceptsShellLinkHeader(string name)
    {
        Assert.True(ShortcutFile.IsValid(name, ShellLinkBytes()));
    }

    [Fact]
    public void IsValid_RejectsDamagedShellLinks()
    {
        byte[] bytes = ShellLinkBytes();
        bytes[10] ^= 0xFF;

        Assert.False(ShortcutFile.IsValid("Steam.lnk", bytes));
        Assert.False(ShortcutFile.IsValid("Steam.lnk", new byte[10]));
        Assert.False(ShortcutFile.IsValid("Steam.lnk", null));
    }

    [Fact]
    public void IsValid_ChecksUrlShortcutsForTheirTarget()
    {
        byte[] url = Encoding.ASCII.GetBytes("[InternetShortcut]\r\nURL=https://store.steampowered.com/\r\n");
        byte[] empty = Encoding.ASCII.GetBytes("[InternetShortcut]\r\nIconIndex=0\r\n");

        Assert.True(ShortcutFile.IsValid("Store.url", url));
        Assert.True(ShortcutFile.IsValid("Store.url.bak", url));
        Assert.False(ShortcutFile.IsValid("Store.url", empty));
    }

    [Fact]
    public void IsValid_RejectsOtherFileTypes()
    {
        Assert.False(ShortcutFile.IsValid("Steam.exe", ShellLinkBytes()));
    }

    private static byte[] ShellLinkBytes()
    {
        var bytes = new byte[76];
        bytes[0] = 0x4C;
        byte[] clsid = [0x01, 0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46];
        clsid.CopyTo(bytes, 4);
        return bytes;
    }
}
