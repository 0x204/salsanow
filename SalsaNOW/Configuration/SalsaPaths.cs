using SalsaNOW.Core.Json;
using SalsaNOW.Core.Settings;
using SalsaNOW.Net;

namespace SalsaNOW.Configuration;

internal sealed class SalsaPaths(string root)
{
    public const string DefaultRoot = @"I:\Apps\SalsaNOW";

    public string Root { get; } = Path.GetFullPath(root);

    public string SilentApps => Path.Combine(Root, "SilentApps");

    public string SavedShortcuts => Path.Combine(Root, "Shortcuts");

    public string DeletedShortcuts => Path.Combine(Root, "Backup Shortcuts");

    public string RegistryBackups => Path.Combine(Root, "RegistryBackups");

    public string RegistryFiles => Path.Combine(Root, "RegistryFiles");

    public string GameSaves => Path.Combine(Root, "Game Saves");

    public string Wallpapers => Path.Combine(Root, "DesktopWallpaper");

    public string DefaultWallpapers => Path.Combine(Wallpapers, "DefaultWallpaper");

    public string ConfigCache => Path.Combine(Root, "Cache");

    public string Temp => Path.Combine(Root, "Temp");

    public string SettingsFile => Path.Combine(Root, "SalsaNOWSettings.json");

    public string StartupBatch => Path.Combine(Root, "StartupBatch.bat");

    public string WelcomeMarker => Path.Combine(Root, "SalsaNOWWelcome.shown");

    public string Waterfox => Path.Combine(Root, "Waterfox", "waterfox.exe");

    public string TranslucentTb => Path.Combine(Root, "Zxplorer", "TranslucentTB");

    public string OpenShell => Path.Combine(SilentApps, "Open-Shell");

    public string PowerShell => Path.Combine(SilentApps, "PowerShell");

    public string SetUserFta => Path.Combine(SilentApps, "SetUserFTA.exe");

    public string InRoot(string relativePath) => Path.Combine(Root, relativePath);

    public static async Task<SalsaPaths> DownloadAsync(CancellationToken cancellationToken = default)
    {
        string json = await Http.GetStringAsync(Endpoints.InstallDirectory, cancellationToken);
        DirectoryEntry? entry = JsonConfig.Parse<List<DirectoryEntry>>(json).FirstOrDefault();
        if (entry is null || string.IsNullOrWhiteSpace(entry.DirectoryCreate))
            throw new InvalidDataException("directory.json does not name a folder.");

        return new SalsaPaths(entry.DirectoryCreate);
    }
}
