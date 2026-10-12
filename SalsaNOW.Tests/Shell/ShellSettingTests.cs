using SalsaNOW.Core.Shell;

namespace SalsaNOW.Tests.Shell;

public class ShellSettingTests
{
    [Theory]
    [InlineData("#FF8800", unchecked((int)0xFF0088FF))]
    [InlineData("3a86ff", unchecked((int)0xFFFF863A))]
    [InlineData(" #000000 ", unchecked((int)0xFF000000))]
    public void AccentColor_ConvertsRgbToAbgr(string hex, int expected)
    {
        Assert.True(AccentColor.TryParse(hex, out int abgr));
        Assert.Equal(expected, abgr);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#FFF")]
    [InlineData("#GG0000")]
    [InlineData("#FF000000")]
    public void AccentColor_RejectsInvalidValues(string? hex)
    {
        Assert.False(AccentColor.TryParse(hex, out _));
    }

    [Fact]
    public void AccentColor_PaletteRepeatsTheColorAsRgba()
    {
        AccentColor.TryParse("#112233", out int abgr);

        byte[] palette = AccentColor.BuildPalette(abgr);

        Assert.Equal(32, palette.Length);
        for (int i = 0; i < 8; i++)
            Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0xFF }, palette.Skip(i * 4).Take(4));
    }

    [Theory]
    [InlineData("left", 0)]
    [InlineData("LEFT", 0)]
    [InlineData("center", 1)]
    public void Alignment(string value, int expected) => Assert.Equal(expected, ShellSettingValues.Alignment(value));

    [Theory]
    [InlineData("always", 0)]
    [InlineData("whenFull", 1)]
    [InlineData("never", 2)]
    [InlineData("unknown", 0)]
    public void Combine(string value, int expected) => Assert.Equal(expected, ShellSettingValues.Combine(value));

    [Theory]
    [InlineData("always", 0)]
    [InlineData("never", 1)]
    [InlineData("whenFull", 2)]
    public void SmallerButtons(string value, int expected) => Assert.Equal(expected, ShellSettingValues.SmallerButtons(value));

    [Theory]
    [InlineData("light", true)]
    [InlineData("Light", true)]
    [InlineData("dark", false)]
    [InlineData(null, false)]
    public void IsLight(string? mode, bool expected) => Assert.Equal(expected, ShellSettingValues.IsLight(mode));
}
