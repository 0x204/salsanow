using SalsaNOW.Infrastructure;

namespace SalsaNOW.Guards;

internal static class AntiCheatWatcher
{
    private static readonly string[] ProcessNames =
        ["EasyAntiCheat_EOS_Setup", "EasyAntiCheat_Setup", "EasyAntiCheat", "EasyAntiCheat_EOS"];

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        Task notice = Task.CompletedTask;

        while (true)
        {
            await Task.Delay(PollInterval, cancellationToken);

            int killed = 0;
            foreach (string name in ProcessNames)
            {
                int killedNow = ProcessRunner.KillAll(name);
                if (killedNow > 0)
                    Log.Warn($"Terminated blocked process: {name}");
                killed += killedNow;
            }

            if (killed > 0 && notice.IsCompleted)
            {
                notice = Dialogs.ShowInfoAsync(
                    "Easy Anti-Cheat processes have been terminated to prevent session issues. Anti-Cheat games don't work.",
                    "SalsaNOW");
            }
        }
    }
}
