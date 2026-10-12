using SalsaNOW.Core.Steam;

namespace SalsaNOW.Tests.Steam;

public class SteamConfigTextTests
{
    private const string LocalConfig = """
        "UserLocalConfigStore"
        {
            "Software"
            {
                "Valve"
                {
                    "Steam"
                    {
                        "apps"
                        {
                            "730"
                            {
                                "LastPlayed"		"1760000000"
                                "LaunchOptions"		"{0}"
                            }
                        }
                    }
                }
            }
        }
        """;

    [Theory]
    [InlineData("-novid")]
    [InlineData("cmd /c start calc %command%")]
    [InlineData("  -high")]
    [InlineData("\\\"C:\\\\x.exe\\\" %command%")]
    public void HasLaunchOptions_DetectsNonEmptyValues(string options)
    {
        Assert.True(SteamConfigText.HasLaunchOptions(LocalConfig.Replace("{0}", options)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HasLaunchOptions_IgnoresClearedOptions(string options)
    {
        Assert.False(SteamConfigText.HasLaunchOptions(LocalConfig.Replace("{0}", options)));
    }

    [Fact]
    public void HasLaunchOptions_IgnoresFilesWithoutTheKey()
    {
        Assert.False(SteamConfigText.HasLaunchOptions("\"LastPlayed\"\t\t\"1760000000\""));
    }

    [Fact]
    public void TrySetSteamInput_TurnsEveryControllerSwitchOff()
    {
        const string config = """
            "SteamController_XBoxSupport"		"1"
            "SteamController_PSSupport"		"1"
            "SteamController_GenericGamepadSupport"		"0"
            """;

        Assert.True(SteamConfigText.TrySetSteamInput(config, enabled: false, out string updated));
        Assert.DoesNotContain("\"1\"", updated);
        Assert.Contains("\"SteamController_PSSupport\"\t\t\"0\"", updated);
    }

    [Fact]
    public void TrySetSteamInput_ReportsNoChangeWhenAlreadySet()
    {
        const string config = "\"SteamController_XBoxSupport\"\t\t\"1\"";

        Assert.False(SteamConfigText.TrySetSteamInput(config, enabled: true, out string updated));
        Assert.Equal(config, updated);
    }

    [Fact]
    public void TrySetSteamInput_IgnoresFilesWithoutControllerSwitches()
    {
        Assert.False(SteamConfigText.TrySetSteamInput("\"LastPlayed\"\t\t\"1\"", enabled: false, out _));
    }

    [Theory]
    [InlineData("SteamAppId", true)]
    [InlineData("SteamGameId", true)]
    [InlineData("VALVE_SHADER_CACHE", true)]
    [InlineData("SDL_GAMECONTROLLER_IGNORE_DEVICES", true)]
    [InlineData("__GL_SHADER_DISK_CACHE", true)]
    [InlineData("Path", false)]
    [InlineData("SDL_VIDEODRIVER", false)]
    public void IsSteamVariable(string name, bool expected)
    {
        Assert.Equal(expected, SteamEnvironmentVariables.IsSteamVariable(name));
    }
}
