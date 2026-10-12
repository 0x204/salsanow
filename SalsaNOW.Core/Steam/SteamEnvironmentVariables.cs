namespace SalsaNOW.Core.Steam;

public static class SteamEnvironmentVariables
{
    private static readonly string[] Prefixes =
    [
        "FOSSILIZE_",
        "MESA_",
        "AMD_VK_",
        "__GL_SHADER_",
        "DXVK_STATE_CACHE",
        "SDL_GAMECONTROLLER_",
    ];

    public static bool IsSteamVariable(string name)
    {
        string upper = name.ToUpperInvariant();
        return upper.Contains("STEAM")
            || upper.Contains("VALVE")
            || Prefixes.Any(prefix => upper.StartsWith(prefix, StringComparison.Ordinal));
    }
}
