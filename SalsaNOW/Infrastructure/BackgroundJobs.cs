namespace SalsaNOW.Infrastructure;

internal static class BackgroundJobs
{
    public static void Start(string name, Func<Task> job)
    {
        Task.Run(job).ContinueWith(
            task => Log.Error($"{name} failed", task.Exception!.GetBaseException()),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    public static void Start(string name, Action job) => Start(name, () => Task.Run(job));
}
