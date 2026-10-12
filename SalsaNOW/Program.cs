using SalsaNOW.Configuration;
using SalsaNOW.Infrastructure;
using SalsaNOW.Recovery;
using SalsaNOW.Steam;

namespace SalsaNOW;

internal static class Program
{
    private const string GeForceNowMarker = @"C:\Asgard";
    private static readonly TimeSpan RecoveryPromptDuration = TimeSpan.FromSeconds(1.5);

    private static async Task<int> Main(string[] args)
    {
        SteamEnvironment.Clear();

        Console.Title = $"SalsaNOW V{AppInfo.Version} - by dpadGuy";
        CommandLineOptions options = CommandLineOptions.Parse(args);

        Console.WriteLine($"SalsaNOW V{AppInfo.Version}");
        Console.WriteLine("IF YOU HAVE PAID FOR SALSANOW ACCESS THEN IT MEANS YOU GOT SCAMMED AND SHOULD DEMAND YOUR MONEY BACK IMMEDIATELY.");
        Console.WriteLine();

        if (!Directory.Exists(GeForceNowMarker))
        {
            Console.WriteLine("[!] Not a GeForce NOW environment. Exiting...");
            await Task.Delay(5000);
            return 0;
        }

        if (WaitForRecoveryKey())
        {
            Console.WriteLine("Recovery mode selected.");
            await RecoveryMenu.RunAsync();
            return 0;
        }

        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            shutdown.Cancel();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            CrashReport.ShowAsync(e.ExceptionObject).GetAwaiter().GetResult();

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error("Unobserved background error", e.Exception.GetBaseException());
            e.SetObserved();
        };

        try
        {
            await SalsaSession.RunAsync(options, shutdown.Token);
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await CrashReport.ShowAsync(ex);
            return 1;
        }

        return 0;
    }

    private static bool WaitForRecoveryKey()
    {
        const string Prompt = "Press DEL key for recovery mode";
        DateTime start = DateTime.Now;

        try
        {
            while (DateTime.Now - start < RecoveryPromptDuration)
            {
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Delete)
                    return true;

                int dots = Math.Min((int)(DateTime.Now - start).TotalSeconds + 1, 3);
                Console.Write($"\r{Prompt}{new string('.', dots)}");
                Thread.Sleep(10);
            }

            return false;
        }
        finally
        {
            ClearLine();
        }
    }

    private static void ClearLine()
    {
        Console.Write("\r" + new string(' ', Math.Max(Console.BufferWidth - 1, 0)) + "\r");
    }
}
