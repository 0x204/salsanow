using System.Net.Http;
using System.Text;
using SalsaNOW.Net;

namespace SalsaNOW.Infrastructure;

internal static class CrashReport
{
    public static async Task ShowAsync(string error)
    {
        Log.Error(error);

        string link;
        try
        {
            link = (await Http.PostTextAsync(Endpoints.CrashLogPaste, BuildReport(error), "SalsaNOW-CrashReporter")).Trim();
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or IOException or OperationCanceledException)
        {
            link = "Could not be uploaded.";
        }

        Dialogs.ShowError(
            $"SalsaNOW just crashed!\n\nError:\n{error}\n\nPlease send this Crashlog to the SalsaNOW Devs:\n{link}",
            "SalsaNOW - Fatal Error");
    }

    private static string BuildReport(string error)
    {
        var report = new StringBuilder()
            .Append("--- SalsaNOW CRASH REPORT ---\n")
            .Append($"Version: {AppInfo.Version}\n")
            .Append($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n")
            .Append($"Error: {error}\n\n");

        string log = Log.ReadAll();
        if (log.Length > 0)
            report.Append("--- LOG HISTORY ---\n").Append(log);

        return report.ToString();
    }
}
