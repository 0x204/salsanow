namespace SalsaNOW.Core.Shell;

public static class ShellSettingValues
{
    public static bool IsLight(string? mode) => string.Equals(mode, "light", StringComparison.OrdinalIgnoreCase);

    public static int Alignment(string value) => string.Equals(value, "left", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

    public static int Combine(string value)
    {
        if (string.Equals(value, "whenFull", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (string.Equals(value, "never", StringComparison.OrdinalIgnoreCase))
            return 2;
        return 0;
    }

    public static int SmallerButtons(string value)
    {
        if (string.Equals(value, "always", StringComparison.OrdinalIgnoreCase))
            return 0;
        if (string.Equals(value, "whenFull", StringComparison.OrdinalIgnoreCase))
            return 2;
        return 1;
    }
}
