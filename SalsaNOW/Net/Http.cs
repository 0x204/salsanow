using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using SalsaNOW.Core.Web;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Net;

internal static class Http
{
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)];

    private const int ErrorHandleDiskFull = 0x27;
    private const int ErrorDiskFull = 0x70;

    private static readonly HttpClient Client = CreateClient();

    public static Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        return WithRetriesAsync(url, async token =>
        {
            using HttpResponseMessage response = await GetAsync(url, token);
            using var content = new MemoryStream();
            await CopyContentAsync(response, content, token);

            content.Position = 0;
            using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }, cancellationToken);
    }

    public static async Task DownloadFileAsync(string url, string destination, CancellationToken cancellationToken = default)
    {
        string partial = destination + ".part";
        try
        {
            await WithRetriesAsync(url, async token =>
            {
                using (HttpResponseMessage response = await GetAsync(url, token))
                using (var file = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await CopyContentAsync(response, file, token);
                }

                return true;
            }, cancellationToken);

            FileSystem.ReplaceFile(partial, destination);
        }
        finally
        {
            FileSystem.TryDeleteFile(partial);
        }
    }

    public static async Task<string> PostTextAsync(string url, string text, string userAgent, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(text, Encoding.UTF8, "text/plain"),
        };
        request.Headers.UserAgent.ParseAdd(userAgent);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ResponseTimeout);

        using HttpResponseMessage response = await Client.SendAsync(request, timeout.Token);
        if (!response.IsSuccessStatusCode)
            throw new HttpStatusException(response.StatusCode, response.ReasonPhrase);

        return await response.Content.ReadAsStringAsync();
    }

    private static HttpClient CreateClient()
    {
        ServicePointManager.DefaultConnectionLimit = 32;
        ServicePointManager.Expect100Continue = false;
        ServicePointManager.UseNagleAlgorithm = false;

        var handler = new WebRequestHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ServerCertificateValidationCallback = CertificateValidation.Validate,
            ReadWriteTimeout = (int)StallTimeout.TotalMilliseconds,
        };

        var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
        return client;
    }

    private static async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ResponseTimeout);

        HttpResponseMessage response;
        try
        {
            response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"No response within {ResponseTimeout.TotalSeconds:0} seconds.");
        }

        if (!response.IsSuccessStatusCode)
        {
            using (response)
                throw new HttpStatusException(response.StatusCode, response.ReasonPhrase);
        }

        return response;
    }

    private static async Task CopyContentAsync(HttpResponseMessage response, Stream destination, CancellationToken cancellationToken)
    {
        using Stream source = await response.Content.ReadAsStreamAsync();
        using var stall = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using CancellationTokenRegistration abort = stall.Token.Register(response.Dispose);

        var buffer = new byte[81920];
        while (true)
        {
            stall.CancelAfter(StallTimeout);

            int read;
            try
            {
                read = await source.ReadAsync(buffer, 0, buffer.Length, stall.Token);
            }
            catch (Exception) when (stall.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException($"No data received for {StallTimeout.TotalSeconds:0} seconds.");
            }

            if (read == 0)
                return;

            await destination.WriteAsync(buffer, 0, read, cancellationToken);
        }
    }

    private static async Task<T> WithRetriesAsync<T>(string url, Func<CancellationToken, Task<T>> attempt, CancellationToken cancellationToken)
    {
        for (int retry = 0; ; retry++)
        {
            try
            {
                return await attempt(cancellationToken);
            }
            catch (Exception ex) when (retry < RetryDelays.Length && IsTransient(ex) && !cancellationToken.IsCancellationRequested)
            {
                Log.Warn($"Download of {UrlText.Describe(url)} failed ({ex.Message}). Retrying.");
                await Task.Delay(RetryDelays[retry], cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception exception) => exception switch
    {
        HttpStatusException status => status.IsTransient,
        HttpRequestException { InnerException: WebException { Status: WebExceptionStatus.TrustFailure } } => false,
        IOException io when IsDiskFull(io) => false,
        HttpRequestException or TimeoutException or IOException or WebException => true,
        _ => false,
    };

    private static bool IsDiskFull(IOException exception) => (exception.HResult & 0xFFFF) is ErrorDiskFull or ErrorHandleDiskFull;
}
