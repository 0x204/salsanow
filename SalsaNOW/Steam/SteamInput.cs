using SalsaNOW.Core.Steam;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Steam;

internal static class SteamInput
{
    public static async Task ApplyAsync(bool enabled)
    {
        if (!Directory.Exists(SteamPaths.UserData))
            return;

        IEnumerable<string> files = Directory.GetFiles(SteamPaths.UserData, "localconfig.vdf", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(SteamPaths.UserData, "config.vdf", SearchOption.AllDirectories));

        await Task.WhenAll(files.Select(file => ApplyToFileAsync(file, enabled)));
    }

    private static async Task ApplyToFileAsync(string file, bool enabled)
    {
        try
        {
            string content;
            using (var reader = new StreamReader(file))
                content = await reader.ReadToEndAsync();

            if (!SteamConfigText.TrySetSteamInput(content, enabled, out string updated))
                return;

            using (var writer = new StreamWriter(file, append: false))
                await writer.WriteAsync(updated);

            Log.Info(enabled
                ? $"Steam Input set to 1 from SalsaNOWSettings.json in {file}"
                : $"Steam Input forced off in {file} because steamInput is not enabled.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Could not update Steam Input in {file}", ex);
        }
    }
}
