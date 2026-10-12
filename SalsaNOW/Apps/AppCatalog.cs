using SalsaNOW.Core.Apps;
using SalsaNOW.Core.Json;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;

namespace SalsaNOW.Apps;

internal static class AppCatalog
{
    public static async Task<List<AppEntry>> LoadAsync(RemoteConfig config, string? customAppsJson, CancellationToken cancellationToken)
    {
        List<AppEntry> apps = await config.LoadAsync<List<AppEntry>>(Endpoints.Apps, cancellationToken);

        if (!string.IsNullOrEmpty(customAppsJson) && File.Exists(customAppsJson))
        {
            try
            {
                apps.AddRange(JsonConfig.Parse<List<AppEntry>>(File.ReadAllText(customAppsJson)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                Log.Error("Custom JSON Error", ex);
            }
        }

        return apps;
    }
}
