using static SalsaNOW.Infrastructure.NativeMethods;

namespace SalsaNOW.Infrastructure;

internal static class WindowTools
{
    public static bool CloseByTitle(string title)
    {
        IntPtr window = FindWindow(null, title);
        if (window == IntPtr.Zero)
            return false;

        return PostMessage(window, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    public static void NotifySettingChange(IntPtr window, string area, uint timeoutMilliseconds = 500)
    {
        SendMessageTimeout(window, WM_SETTINGCHANGE, IntPtr.Zero, area, SMTO_ABORTIFHUNG, timeoutMilliseconds, out _);
    }

    public static void BroadcastSettingChange(string area, uint timeoutMilliseconds = 500) =>
        NotifySettingChange(HWND_BROADCAST, area, timeoutMilliseconds);

    public static void NotifySettingChange(string windowClass, string area)
    {
        IntPtr window = FindWindow(windowClass, null);
        if (window != IntPtr.Zero)
            NotifySettingChange(window, area);
    }

    public static void RoundCorners(IntPtr window)
    {
        int preference = DWMWCP_ROUND;
        try
        {
            DwmSetWindowAttribute(window, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
    }

    public static void HideConsole() => ShowWindow(GetConsoleWindow(), SW_HIDE);

    public static void ShowConsole() => ShowWindow(GetConsoleWindow(), SW_SHOW);
}
