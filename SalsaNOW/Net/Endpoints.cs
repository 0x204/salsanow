namespace SalsaNOW.Net;

internal static class Endpoints
{
    private const string Files = "https://salsanowfiles.work";

    public const string InstallDirectory = Files + "/jsons/directory.json";
    public const string Apps = Files + "/jsons/appsV2.json";
    public const string SilentApps = Files + "/jsons/silentappsV2.json";
    public const string GameSavePaths = Files + "/jsons/GameSavesPaths.json";

    public const string Desktops = Files + "/dev/jsons/ExplorerDesktop.json";

    public const string RegistryBackupList = Files + "/ExplorerContents/jsons/RegistryList.json";
    public const string RegistryFiles = Files + "/ExplorerContents/jsons/RegistryFiles.json";
    public const string DefaultWallpaper = Files + "/ExplorerContents/Wallpaper/WallpaperWin11.jpg";

    public const string SteamProxyBypass = Files + "/USG/bleh.exe";
    public const string SteamUiChunk = Files + "/USG/chunk~2dcc5aaf7.js";

    public const string ExplorerPlusPlus = Files + "/exes/Explorer%2B%2B.exe";
    public const string SystemInformer = Files + "/zips/System.Informer.zip";

    public const string Bing = "https://www.bing.com";
    public const string BingImageOfTheDay = Bing + "/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=";

    public const string CrashLogPaste = "https://paste.rs/";
}
