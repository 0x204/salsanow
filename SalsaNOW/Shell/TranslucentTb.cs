using System.ComponentModel;
using SalsaNOW.Core.Settings;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Shell;

internal sealed class TranslucentTb(string folder)
{
    private const string ProcessName = "TranslucentTB";

    private const string ClearTaskbarSettings = """
        {
          "$schema": "https://translucenttb.github.io/settings.schema.json",
          "desktop_appearance": {
            "accent": "clear",
            "color": "#00000000",
            "show_line": false
          },
          "visible_window_appearance": {
            "enabled": false
          },
          "maximized_window_appearance": {
            "enabled": false
          },
          "start_opened_appearance": {
            "enabled": false
          },
          "search_opened_appearance": {
            "enabled": false
          },
          "task_view_opened_appearance": {
            "enabled": false
          },
          "battery_saver_appearance": {
            "enabled": false
          },
          "hide_tray": true,
          "disable_saving": true,
          "verbosity": "off",
          "use_xaml_context_menu": false
        }
        """;

    private string Executable => Path.Combine(folder, "TranslucentTB.exe");

    public bool Prepare(UserSettings settings)
    {
        if (settings.TaskbarFullTransparency is not bool enabled)
            return false;

        try
        {
            if (!enabled)
            {
                Stop();
                return false;
            }

            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "settings.json"), ClearTaskbarSettings + Environment.NewLine);
            if (!File.Exists(Executable))
            {
                Log.Warn("TranslucentTB.exe not found: " + Executable);
                return false;
            }

            Stop();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("SalsaNOWSettings.json TranslucentTB failed", ex);
            return false;
        }
    }

    public void Start()
    {
        if (!File.Exists(Executable))
            return;

        try
        {
            Stop();
            ProcessRunner.Launch(Executable, workingDirectory: folder)?.Dispose();
            Log.Info("SalsaNOWSettings.json TranslucentTB started.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Log.Error("SalsaNOWSettings.json TranslucentTB failed", ex);
        }
    }

    private static void Stop() => ProcessRunner.KillAll(ProcessName, waitForExit: TimeSpan.FromSeconds(3));
}
