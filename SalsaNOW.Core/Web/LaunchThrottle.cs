namespace SalsaNOW.Core.Web;

public sealed class LaunchThrottle(TimeSpan cooldown)
{
    private static readonly TimeSpan Retention = TimeSpan.FromSeconds(30);

    private readonly Dictionary<string, DateTime> _lastLaunches = new(StringComparer.OrdinalIgnoreCase);

    public bool TryBegin(string url, DateTime now)
    {
        Prune(now);

        if (_lastLaunches.TryGetValue(url, out DateTime last) && now - last < cooldown)
            return false;

        _lastLaunches[url] = now;
        return true;
    }

    private void Prune(DateTime now)
    {
        foreach (string url in _lastLaunches.Where(entry => now - entry.Value > Retention).Select(entry => entry.Key).ToList())
            _lastLaunches.Remove(url);
    }
}
