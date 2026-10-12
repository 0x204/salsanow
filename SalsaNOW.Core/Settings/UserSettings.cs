using Newtonsoft.Json;

namespace SalsaNOW.Core.Settings;

public sealed class UserSettings
{
    [JsonProperty("setUserFta")]
    public string? SetUserFta { get; set; }

    [JsonProperty("defaultBrowser")]
    public string? DefaultBrowser { get; set; }

    [JsonProperty("associations")]
    public List<FileAssociation>? Associations { get; set; }

    [JsonProperty("colorMode")]
    public string? ColorMode { get; set; }

    [JsonProperty("windowsMode")]
    public string? WindowsMode { get; set; }

    [JsonProperty("appsMode")]
    public string? AppsMode { get; set; }

    [JsonProperty("transparency")]
    public bool? Transparency { get; set; }

    [JsonProperty("accentSourceAutomatic")]
    public bool AccentSourceAutomatic { get; set; }

    [JsonProperty("accentColor")]
    public string? AccentColor { get; set; }

    [JsonProperty("taskbarAlignment")]
    public string? TaskbarAlignment { get; set; }

    [JsonProperty("taskbarAutoHide")]
    public bool? TaskbarAutoHide { get; set; }

    [JsonProperty("taskbarBadges")]
    public bool? TaskbarBadges { get; set; }

    [JsonProperty("taskbarFlashing")]
    public bool? TaskbarFlashing { get; set; }

    [JsonProperty("taskbarShareWindows")]
    public bool? TaskbarShareWindows { get; set; }

    [JsonProperty("taskbarShowDesktopCorner")]
    public bool? TaskbarShowDesktopCorner { get; set; }

    [JsonProperty("taskbarCombine")]
    public string? TaskbarCombine { get; set; }

    [JsonProperty("taskbarSmallerButtons")]
    public string? TaskbarSmallerButtons { get; set; }

    [JsonProperty("taskbarFullTransparency")]
    public bool? TaskbarFullTransparency { get; set; }

    [JsonProperty("steamSilentLaunch")]
    public bool? SteamSilentLaunch { get; set; }

    [JsonProperty("steamInput")]
    public bool? SteamInput { get; set; }

    [JsonProperty("bingWallpaper")]
    public bool? BingWallpaper { get; set; }
}

public sealed class FileAssociation
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("progId")]
    public string? ProgId { get; set; }

    [JsonProperty("application")]
    public string? Application { get; set; }
}
