using System.Diagnostics;
using System.Reflection;

namespace SalsaNOW;

internal static class AppInfo
{
    public static string Version { get; } =
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    public static string ExecutablePath
    {
        get
        {
            using Process current = Process.GetCurrentProcess();
            return current.MainModule!.FileName;
        }
    }
}
