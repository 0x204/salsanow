using System.Runtime.InteropServices;
using Microsoft.Win32;
using SalsaNOW.Core.Settings;
using SalsaNOW.Core.Shell;
using SalsaNOW.Infrastructure;
using static SalsaNOW.Infrastructure.NativeMethods;

namespace SalsaNOW.Shell;

internal static class Appearance
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string DwmKey = @"Software\Microsoft\Windows\DWM";
    private const string AccentKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";
    private const string DesktopKey = @"Control Panel\Desktop";
    private const string AdvancedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string StuckRectsKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3";

    public static void ApplyDefaults()
    {
        WriteDarkTheme();
        SetTaskbarAlignment(left: true);
    }

    public static void WriteDarkTheme()
    {
        UserRegistry.SetDword(PersonalizeKey, "SystemUsesLightTheme", 0);
        UserRegistry.SetDword(PersonalizeKey, "AppsUseLightTheme", 0);
        DisableAccentOnSurfaces();
    }

    public static void SetTaskbarAlignment(bool left) => SetAdvanced("TaskbarAl", left ? 0 : 1);

    public static void WriteThemeMode(UserSettings settings)
    {
        if (string.Equals(settings.ColorMode, "custom", StringComparison.OrdinalIgnoreCase))
        {
            UserRegistry.SetDword(PersonalizeKey, "SystemUsesLightTheme", ShellSettingValues.IsLight(settings.WindowsMode) ? 1 : 0);
            UserRegistry.SetDword(PersonalizeKey, "AppsUseLightTheme", ShellSettingValues.IsLight(settings.AppsMode) ? 1 : 0);
        }
        else if (!string.IsNullOrWhiteSpace(settings.ColorMode))
        {
            int light = ShellSettingValues.IsLight(settings.ColorMode) ? 1 : 0;
            UserRegistry.SetDword(PersonalizeKey, "SystemUsesLightTheme", light);
            UserRegistry.SetDword(PersonalizeKey, "AppsUseLightTheme", light);
        }

        if (settings.Transparency is bool transparency)
            UserRegistry.SetDword(PersonalizeKey, "EnableTransparency", transparency ? 1 : 0);

        DisableAccentOnSurfaces();
    }

    public static void WriteAccent(UserSettings settings)
    {
        if (settings.AccentSourceAutomatic)
        {
            UserRegistry.SetDword(DesktopKey, "AutoColorization", 1);
            return;
        }

        UserRegistry.SetDword(DesktopKey, "AutoColorization", 0);
        if (AccentColor.TryParse(settings.AccentColor, out int abgr))
        {
            UserRegistry.SetDword(AccentKey, "AccentColor", abgr);
            UserRegistry.SetDword(AccentKey, "AccentColorMenu", abgr);
            UserRegistry.SetBinary(AccentKey, "AccentPalette", AccentColor.BuildPalette(abgr));
        }
    }

    public static void WriteTaskbar(UserSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.TaskbarAlignment))
            SetAdvanced("TaskbarAl", ShellSettingValues.Alignment(settings.TaskbarAlignment!));

        if (settings.TaskbarBadges is bool badges)
            SetAdvanced("TaskbarBadges", badges ? 1 : 0);

        if (settings.TaskbarFlashing is bool flashing)
            SetAdvanced("TaskbarFlashing", flashing ? 1 : 0);

        if (settings.TaskbarShareWindows is bool shareWindows)
            SetAdvanced("TaskbarSn", shareWindows ? 1 : 0);

        if (settings.TaskbarShowDesktopCorner is bool showDesktop)
            SetAdvanced("TaskbarSd", showDesktop ? 1 : 0);

        if (!string.IsNullOrWhiteSpace(settings.TaskbarCombine))
            SetAdvanced("TaskbarGlomLevel", ShellSettingValues.Combine(settings.TaskbarCombine!));

        if (!string.IsNullOrWhiteSpace(settings.TaskbarSmallerButtons))
        {
            int preference = ShellSettingValues.SmallerButtons(settings.TaskbarSmallerButtons!);
            SetAdvanced("IconSizePreference", preference);
            if (preference == 0)
                SetAdvanced("TaskbarSi", 0);
        }

        if (settings.TaskbarAutoHide is bool autoHide)
            SetAutoHide(autoHide);
    }

    public static void NotifyThemeChanged()
    {
        WindowTools.BroadcastSettingChange("ImmersiveColorSet");
        WindowTools.NotifySettingChange("Shell_TrayWnd", "ImmersiveColorSet");
        WindowTools.NotifySettingChange("Progman", "ImmersiveColorSet");
    }

    public static void NotifyTaskbarChanged()
    {
        WindowTools.NotifySettingChange("Shell_TrayWnd", "TraySettings");
        WindowTools.BroadcastSettingChange("TraySettings");
    }

    private static void DisableAccentOnSurfaces()
    {
        UserRegistry.SetDword(PersonalizeKey, "ColorPrevalence", 0);
        UserRegistry.SetDword(DwmKey, "ColorPrevalence", 0);
        UserRegistry.SetDword(DwmKey, "EnableWindowColorization", 0);
    }

    private static void SetAdvanced(string name, int value) => UserRegistry.SetDword(AdvancedKey, name, value);

    private static void SetAutoHide(bool hide)
    {
        IntPtr tray = FindWindow("Shell_TrayWnd", null);
        if (tray != IntPtr.Zero)
        {
            var data = new APPBARDATA { cbSize = Marshal.SizeOf<APPBARDATA>(), hWnd = tray };
            uint state = SHAppBarMessage(ABM_GETSTATE, ref data);
            data.lParam = new IntPtr((state & ABS_ALWAYSONTOP) | (hide ? ABS_AUTOHIDE : 0));
            SHAppBarMessage(ABM_SETSTATE, ref data);
        }

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(StuckRectsKey, writable: true);
        if (key?.GetValue("Settings") is byte[] { Length: >= 9 } stuckRects)
        {
            stuckRects[8] = hide ? (byte)(stuckRects[8] | 0x01) : (byte)(stuckRects[8] & ~0x01);
            key.SetValue("Settings", stuckRects, RegistryValueKind.Binary);
        }
    }
}
