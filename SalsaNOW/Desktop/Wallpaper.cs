using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SalsaNOW.Configuration;
using SalsaNOW.Core.Json;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;
using static SalsaNOW.Infrastructure.NativeMethods;

namespace SalsaNOW.Desktop;

internal static class Wallpaper
{
    private static readonly string[] SupportedExtensions =
        [".bmp", ".jpg", ".jpeg", ".png", ".gif", ".tif", ".tiff", ".webp", ".jxr"];

    public static async Task<string?> ChooseAsync(SalsaPaths paths, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(paths.Wallpapers);
        Directory.CreateDirectory(paths.DefaultWallpapers);

        string? userWallpaper = Directory.EnumerateFiles(paths.Wallpapers).FirstOrDefault(IsSupportedImage);
        if (userWallpaper is not null)
            return userWallpaper;

        string defaultWallpaper = Path.Combine(paths.DefaultWallpapers, "WallpaperWin11.jpg");
        try
        {
            await Http.DownloadFileAsync(Endpoints.DefaultWallpaper, defaultWallpaper, cancellationToken);
            Log.Info("No user wallpaper set. Downloaded default wallpaper.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("Failed to download default wallpaper", ex);
        }

        return File.Exists(defaultWallpaper) ? defaultWallpaper : null;
    }

    public static async Task<string?> DownloadBingImageAsync(string directory, CancellationToken cancellationToken)
    {
        try
        {
            string market = CultureInfo.CurrentUICulture.Name;
            if (string.IsNullOrWhiteSpace(market))
                market = "en-US";

            string json = await Http.GetStringAsync(Endpoints.BingImageOfTheDay + Uri.EscapeDataString(market), cancellationToken);
            string? urlBase = JsonConfig.Parse<BingImageArchive>(json).Images.FirstOrDefault()?.UrlBase;
            if (string.IsNullOrEmpty(urlBase))
                return null;

            string path = Path.Combine(directory, "wallpaper.jpg");
            await Http.DownloadFileAsync(Endpoints.Bing + urlBase + "_UHD.jpg", path, cancellationToken);
            return path;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warn("Could not download the Bing wallpaper of the day: " + ex.Message);
            return null;
        }
    }

    public static async Task ApplyWhenDesktopReadyAsync(string path, CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (FindWindow("Shell_TrayWnd", null) != IntPtr.Zero && FindWindow("Progman", null) != IntPtr.Zero)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                break;
            }

            await Task.Delay(250, cancellationToken);
        }

        Apply(path);
    }

    public static void Apply(string path)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
            key?.SetValue("Wallpaper", path);
            key?.SetValue("WallpaperStyle", "10");
            key?.SetValue("TileWallpaper", "0");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            Log.Error("Failed to set wallpaper registry", ex);
        }

        bool applied = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE)
            || TrySetThroughDesktopWallpaper(path);

        if (applied)
            Log.Info("Wallpaper applied: " + path);
        else
            Log.Error("Wallpaper was not applied: " + path);
    }

    private static bool IsSupportedImage(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static bool TrySetThroughDesktopWallpaper(string path)
    {
        try
        {
            var desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaperCoClass();
            desktopWallpaper.SetWallpaper(null, path);
            return true;
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or ArgumentException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    private sealed class BingImageArchive
    {
        public List<BingImage> Images { get; set; } = [];
    }

    private sealed class BingImage
    {
        public string? UrlBase { get; set; }
    }

    [ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    private class DesktopWallpaperCoClass
    {
    }

    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
    }
}
