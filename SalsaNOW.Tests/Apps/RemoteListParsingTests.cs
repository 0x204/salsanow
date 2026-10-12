using SalsaNOW.Core.Apps;
using SalsaNOW.Core.Json;
using SalsaNOW.Core.Persistence;
using SalsaNOW.Core.Settings;

namespace SalsaNOW.Tests.Apps;

public class RemoteListParsingTests
{
    [Fact]
    public void ParsesAppList()
    {
        const string json = """
            [
              {
                "name": "7-Zip",
                "fileExtension": "zip",
                "exeName": "7zFM.exe",
                "run": "false",
                "url": "https://salsanowfiles.work/zipsV2/7-Zip.zip",
                "version": "2026.09.03"
              },
              {
                "name": "Explorer++",
                "fileExtension": "exe",
                "exeName": "Explorer++.exe",
                "run": "true",
                "url": "https://salsanowfiles.work/exes/Explorer%2B%2B.exe"
              }
            ]
            """;

        List<AppEntry> apps = JsonConfig.Parse<List<AppEntry>>(json);

        Assert.Equal(2, apps.Count);
        Assert.True(apps[0].IsArchive);
        Assert.False(apps[0].Run);
        Assert.Equal("2026.09.03", apps[0].Version);
        Assert.True(apps[1].IsExecutable);
        Assert.True(apps[1].Run);
        Assert.Null(apps[1].Version);
    }

    [Fact]
    public void ParsesSilentAppList()
    {
        const string json = """
            [
              { "name": "CtrlTab", "fileExtension": "exe", "fileName": "CtrlTab", "run": "true", "url": "https://x/ctrl_tab.exe", "version": "1.0.0" },
              { "name": "Powershell", "fileExtension": "exe", "fileName": "pwsh", "archive": "true", "run": "false", "url": "https://x/pwsh.zip" }
            ]
            """;

        List<SilentAppEntry> apps = JsonConfig.Parse<List<SilentAppEntry>>(json);

        Assert.True(apps[0].Run);
        Assert.False(apps[0].Archive);
        Assert.Equal("CtrlTab.exe", apps[0].ExecutableName);
        Assert.True(apps[1].Archive);
        Assert.Equal("pwsh.exe", apps[1].ExecutableName);
    }

    [Fact]
    public void ParsesDesktopListWithTrailingComma()
    {
        const string json = """
            [
              {
                "name": "Zxplorer",
                "exeName": "StartDesktop.exe",
                "taskbarFixer": "no",
                "run": "true",
                "url": "https://salsanowfiles.work/ExplorerContents/versions/Zxplorer-portable-20260928-1533.zip"
              },
            ]
            """;

        DesktopEntry desktop = Assert.Single(JsonConfig.Parse<List<DesktopEntry>>(json));

        Assert.True(desktop.Run);
        Assert.Equal("Zxplorer-portable-20260928-1533.zip", desktop.ReleaseFileName);
    }

    [Fact]
    public void ParsesRegistryBackupList()
    {
        const string json = """
            {
              "backupIntervalSeconds": 5,
              "registryKeys": [
                { "key": "HKCU\\Software", "file": "Software.reg" },
                { "key": "HKCU\\Software\\Explorer++", "file": "Explorerplusplus.reg" }
              ]
            }
            """;

        RegistryBackupList list = JsonConfig.Parse<RegistryBackupList>(json);

        Assert.Equal(5, list.BackupIntervalSeconds);
        Assert.Equal(2, list.RegistryKeys.Count);
        Assert.Equal(@"HKCU\Software", list.RegistryKeys[0].Key);
        Assert.Equal("Software.reg", list.RegistryKeys[0].File);
    }

    [Fact]
    public void ParsesRegistryFilesAndGameSavePaths()
    {
        RegistryFileEntry file = Assert.Single(JsonConfig.Parse<List<RegistryFileEntry>>(
            """[ { "File": "7zip.reg", "Url": "https://x/7zip.reg" }, ]"""));
        GameSavePathList saves = JsonConfig.Parse<GameSavePathList>(
            """{ "paths": [ "C:\\Users\\Public\\Documents", "C:\\Program Files (x86)\\Steam\\steam\\games" ] }""");

        Assert.Equal("7zip.reg", file.File);
        Assert.Equal(2, saves.Paths.Count);
    }

    [Fact]
    public void ParsesDirectoryList()
    {
        DirectoryEntry entry = Assert.Single(JsonConfig.Parse<List<DirectoryEntry>>(
            """[ { "directoryCreate": "I:\\Apps\\SalsaNOW" } ]"""));

        Assert.Equal(@"I:\Apps\SalsaNOW", entry.DirectoryCreate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{ not json")]
    public void RejectsEmptyOrInvalidDocuments(string json)
    {
        Assert.Throws<InvalidDataException>(() => JsonConfig.Parse<List<AppEntry>>(json));
    }

    [Theory]
    [InlineData("\"true\"", true)]
    [InlineData("\"TRUE\"", true)]
    [InlineData("true", true)]
    [InlineData("\"false\"", false)]
    [InlineData("\"\"", false)]
    [InlineData("null", false)]
    [InlineData("1", false)]
    [InlineData("{ \"nested\": true }", false)]
    public void ReadsStringFlags(string value, bool expected)
    {
        var app = JsonConfig.Parse<AppEntry>($$"""{ "run": {{value}}, "name": "after" }""");

        Assert.Equal(expected, app.Run);
        Assert.Equal("after", app.Name);
    }
}
