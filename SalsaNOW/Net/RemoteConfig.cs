using SalsaNOW.Core.Json;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Net;

internal sealed class RemoteConfig(string cacheDirectory)
{
    public async Task<T> LoadAsync<T>(string url, CancellationToken cancellationToken = default, TimeSpan? timeLimit = null) where T : class
    {
        string cacheFile = Path.Combine(cacheDirectory, CacheFileName(url));

        using var download = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeLimit is TimeSpan limit && File.Exists(cacheFile))
            download.CancelAfter(limit);

        try
        {
            string json = await Http.GetStringAsync(url, download.Token);
            T value = JsonConfig.Parse<T>(json);
            SaveCopy(cacheFile, json);
            return value;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && File.Exists(cacheFile))
        {
            string reason = ex is OperationCanceledException ? "timed out" : ex.Message;
            Log.Warn($"Could not load {DisplayName(url)} ({reason}). Using the copy from the last successful start.");
            return JsonConfig.Parse<T>(File.ReadAllText(cacheFile));
        }
    }

    private static void SaveCopy(string cacheFile, string json)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
            FileSystem.WriteAllTextAtomic(cacheFile, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Could not cache {Path.GetFileName(cacheFile)}: {ex.Message}");
        }
    }

    private static string CacheFileName(string url) => new Uri(url).AbsolutePath.Trim('/').Replace('/', '_');

    private static string DisplayName(string url) => Path.GetFileName(new Uri(url).AbsolutePath);
}
