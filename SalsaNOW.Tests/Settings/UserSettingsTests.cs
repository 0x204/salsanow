using SalsaNOW.Core.Json;
using SalsaNOW.Core.Settings;

namespace SalsaNOW.Tests.Settings;

public class UserSettingsTests
{
    [Fact]
    public void ParsesSettingsFile()
    {
        const string json = """
            {
              "setUserFta": "I:\\Apps\\SalsaNOW\\SilentApps\\SetUserFTA.exe",
              "defaultBrowser": "I:\\Apps\\SalsaNOW\\Waterfox\\waterfox.exe",
              "associations": [ { "type": ".txt", "progId": "NPP.Custom", "application": "I:\\npp.exe" } ],
              "colorMode": "custom",
              "windowsMode": "dark",
              "appsMode": "light",
              "transparency": true,
              "accentSourceAutomatic": false,
              "accentColor": "#3A86FF",
              "taskbarAlignment": "center",
              "taskbarAutoHide": false,
              "taskbarCombine": "whenFull",
              "taskbarSmallerButtons": "never",
              "taskbarFullTransparency": true,
              "steamSilentLaunch": true,
              "steamInput": false,
              "bingWallpaper": true
            }
            """;

        UserSettings settings = JsonConfig.Parse<UserSettings>(json);

        Assert.Equal("custom", settings.ColorMode);
        Assert.Equal("light", settings.AppsMode);
        Assert.True(settings.Transparency);
        Assert.Equal("#3A86FF", settings.AccentColor);
        Assert.False(settings.TaskbarAutoHide);
        Assert.Null(settings.TaskbarBadges);
        Assert.True(settings.SteamSilentLaunch);
        Assert.False(settings.SteamInput);
        Assert.True(settings.BingWallpaper);

        FileAssociation association = Assert.Single(settings.Associations!);
        Assert.Equal(".txt", association.Type);
        Assert.Equal("NPP.Custom", association.ProgId);
    }

    [Fact]
    public void ParsesFolderAndSilentAppOptions()
    {
        const string json = """
            {
              "persistUserFolders": false,
              "disabledSilentApps": [ "CtrlTab", "WindowsMenuToggle" ],
              "ctrlWinApp": "I:\\Apps\\Flow Launcher\\Flow.Launcher.exe"
            }
            """;

        UserSettings settings = JsonConfig.Parse<UserSettings>(json);

        Assert.False(settings.PersistUserFolders);
        string[] expected = ["CtrlTab", "WindowsMenuToggle"];
        Assert.Equal(expected, settings.DisabledSilentApps);
        Assert.Equal(@"I:\Apps\Flow Launcher\Flow.Launcher.exe", settings.CtrlWinApp);
    }

    [Fact]
    public void MissingOptionsStayUnset()
    {
        UserSettings settings = JsonConfig.Parse<UserSettings>("{}");

        Assert.Null(settings.ColorMode);
        Assert.Null(settings.TaskbarFullTransparency);
        Assert.Null(settings.Associations);
        Assert.False(settings.AccentSourceAutomatic);
        Assert.Null(settings.PersistUserFolders);
        Assert.Null(settings.DisabledSilentApps);
        Assert.Null(settings.CtrlWinApp);
    }
}
