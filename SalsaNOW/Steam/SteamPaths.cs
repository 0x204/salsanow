namespace SalsaNOW.Steam;

internal static class SteamPaths
{
    public const string Root = @"C:\Program Files (x86)\Steam";

    public static readonly string Executable = Path.Combine(Root, "steam.exe");

    public static readonly string UserData = Path.Combine(Root, "userdata");

    public static readonly string SteamUi = Path.Combine(Root, "steamui");

    public static readonly string AppCache = Path.Combine(Root, "appcache");

    public static readonly string Games = Path.Combine(Root, "steam", "games");
}
