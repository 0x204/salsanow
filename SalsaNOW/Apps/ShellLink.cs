using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Apps;

internal static class ShellLink
{
    private const int NormalWindow = 1;

    public static void Create(string shortcutPath, string target, string workingDirectory, bool keepCustomizations = true)
    {
        Customizations customizations = keepCustomizations && File.Exists(shortcutPath)
            ? ReadCustomizations(shortcutPath)
            : Customizations.None;

        for (int attempt = 0; attempt < 5 && File.Exists(shortcutPath); attempt++)
        {
            if (!FileSystem.TryDeleteFile(shortcutPath))
                Thread.Sleep(200);
        }

        try
        {
            UseShortcut(shortcutPath, link =>
            {
                link.TargetPath = target;
                link.Arguments = customizations.Arguments;
                link.WorkingDirectory = workingDirectory;
                link.WindowStyle = customizations.WindowStyle;
                if (customizations.Description.Length > 0)
                    link.Description = customizations.Description;
                if (customizations.Hotkey.Length > 0)
                    link.Hotkey = customizations.Hotkey;
                if (customizations.IconLocation.Length > 0)
                    link.IconLocation = customizations.IconLocation;
                link.Save();
            });
        }
        catch (Exception ex) when (IsComFailure(ex))
        {
            Log.Error($"Shortcut creation failed for {Path.GetFileNameWithoutExtension(shortcutPath)}", ex);
        }
    }

    private static Customizations ReadCustomizations(string shortcutPath)
    {
        try
        {
            Customizations result = Customizations.None;
            UseShortcut(shortcutPath, link => result = new Customizations(
                Convert.ToString(link.Arguments) ?? "",
                Convert.ToString(link.IconLocation) ?? "",
                Convert.ToString(link.Description) ?? "",
                Convert.ToString(link.Hotkey) ?? "",
                Convert.ToInt32(link.WindowStyle)));
            return result;
        }
        catch (Exception ex) when (IsComFailure(ex) || ex is FormatException)
        {
            return Customizations.None;
        }
    }

    private static void UseShortcut(string shortcutPath, Action<dynamic> use)
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell", throwOnError: true)!;
        object shell = Activator.CreateInstance(shellType)!;
        object? link = null;
        try
        {
            link = ((dynamic)shell).CreateShortcut(shortcutPath);
            use(link!);
        }
        finally
        {
            if (link is not null)
                Marshal.FinalReleaseComObject(link);
            Marshal.FinalReleaseComObject(shell);
        }
    }

    private static bool IsComFailure(Exception exception) =>
        exception is COMException or UnauthorizedAccessException or InvalidCastException or ArgumentException or RuntimeBinderException;

    private sealed record Customizations(string Arguments, string IconLocation, string Description, string Hotkey, int WindowStyle)
    {
        public static readonly Customizations None = new("", "", "", "", NormalWindow);
    }
}
