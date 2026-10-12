using Microsoft.Win32;
using SalsaNOW.Configuration;
using SalsaNOW.Core.IO;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Runtimes;

internal static class EnvironmentVariables
{
    public static void Configure(SalsaPaths paths)
    {
        Log.Info("Setting up environment variables...");

        string dotnetRoot = DotNetRuntimeInstaller.InstallDirectory;
        string powershellRoot = paths.PowerShell;

        using (RegistryKey? key = Registry.CurrentUser.CreateSubKey("Environment"))
        {
            if (key is null)
            {
                Log.Error(@"Could not open HKCU\Environment.");
                return;
            }

            key.SetValue("DOTNET_ROOT", dotnetRoot, RegistryValueKind.String);
            key.SetValue("POWERSHELL_ROOT", powershellRoot, RegistryValueKind.String);

            string path = key.GetValue("Path", "", RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "";
            path = PathList.AddIfMissing(path, dotnetRoot);
            path = PathList.AddIfMissing(path, powershellRoot);
            key.SetValue("Path", path, RegistryValueKind.ExpandString);
        }

        Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnetRoot);
        Environment.SetEnvironmentVariable("POWERSHELL_ROOT", powershellRoot);

        string processPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        processPath = PathList.AddIfMissing(processPath, dotnetRoot);
        processPath = PathList.AddIfMissing(processPath, powershellRoot);
        Environment.SetEnvironmentVariable("PATH", processPath);

        WindowTools.BroadcastSettingChange("Environment", timeoutMilliseconds: 5000);
        Log.Info("Environment setup complete.");
    }
}
