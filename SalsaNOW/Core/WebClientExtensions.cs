using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SalsaNOW
{
    internal static class WebClientExtensions
    {
        private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(60);

        public static Task<string> DownloadStringWithTimeoutAsync(this WebClient client, string address)
        {
            return WithStallTimeout(client, () => client.DownloadStringTaskAsync(address));
        }

        public static Task DownloadFileWithTimeoutAsync(this WebClient client, string address, string fileName)
        {
            return DownloadFileWithTimeoutAsync(client, new Uri(address), fileName);
        }

        public static Task DownloadFileWithTimeoutAsync(this WebClient client, Uri address, string fileName)
        {
            return WithStallTimeout(client, async () =>
            {
                await client.DownloadFileTaskAsync(address, fileName);
                return true;
            });
        }

        private static async Task<T> WithStallTimeout<T>(WebClient client, Func<Task<T>> start)
        {
            long lastProgress = DateTime.UtcNow.Ticks;
            DownloadProgressChangedEventHandler onProgress =
                (sender, e) => Interlocked.Exchange(ref lastProgress, DateTime.UtcNow.Ticks);

            client.DownloadProgressChanged += onProgress;
            try
            {
                Task<T> transfer = start();
                while (!transfer.IsCompleted)
                {
                    await Task.WhenAny(transfer, Task.Delay(1000));

                    bool stalled = DateTime.UtcNow.Ticks - Interlocked.Read(ref lastProgress) > StallTimeout.Ticks;
                    if (stalled && !transfer.IsCompleted)
                    {
                        client.CancelAsync();
                        _ = transfer.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                        throw new TimeoutException($"No data received for {StallTimeout.TotalSeconds:0} seconds.");
                    }
                }

                return await transfer;
            }
            finally
            {
                client.DownloadProgressChanged -= onProgress;
            }
        }
    }
}
