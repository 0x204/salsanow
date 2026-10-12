using System.ComponentModel;
using SalsaNOW.Configuration;
using SalsaNOW.Core.IO;
using SalsaNOW.Core.Settings;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Shell;

internal static class FileAssociations
{
    private static readonly string[] NotepadPlusPlusTypes =
        [".txt", ".log", ".err", ".ini", ".conf", ".cfg", ".xml", ".json", ".yaml", ".yml", ".md"];

    private static readonly string[] PeaZipTypes =
        [".zip", ".7z", ".rar", ".tar", ".gz", ".tgz", ".bz2", ".xz"];

    public static void ApplyDefaults(SalsaPaths paths)
    {
        if (!File.Exists(paths.SetUserFta))
        {
            Log.Warn("SetUserFTA.exe not found. Skipping file associations.");
            return;
        }

        RegisterOpenCommand("NPP.Custom", paths.InRoot(@"Notepad++\notepad++.exe"));
        RegisterOpenCommand("PZ.Custom", paths.InRoot(@"PeaZip File Explorer Archiver\PeaZip File Explorer Archiver.exe"));

        var associations = NotepadPlusPlusTypes.Select(type => (type, "NPP.Custom"))
            .Concat(PeaZipTypes.Select(type => (type, "PZ.Custom")))
            .ToList();

        Apply(paths.SetUserFta, associations);
        Log.Info("File associations applied.");
    }

    public static void ApplyFromSettings(UserSettings settings)
    {
        if (settings.Associations is not { Count: > 0 })
            return;

        if (string.IsNullOrWhiteSpace(settings.SetUserFta) || !File.Exists(settings.SetUserFta))
        {
            Log.Warn("SetUserFTA.exe from SalsaNOWSettings.json was not found. Skipping configured associations.");
            return;
        }

        var associations = new List<(string Type, string ProgId)>();
        foreach (FileAssociation association in settings.Associations)
        {
            if (string.IsNullOrWhiteSpace(association.Type)
                || string.IsNullOrWhiteSpace(association.ProgId)
                || string.IsNullOrWhiteSpace(association.Application))
            {
                continue;
            }

            RegisterOpenCommand(association.ProgId!, association.Application!);
            associations.Add((association.Type!, association.ProgId!));
        }

        Apply(settings.SetUserFta!, associations);
        Log.Info("SalsaNOWSettings.json associations applied.");
    }

    private static void RegisterOpenCommand(string progId, string executable)
    {
        if (!File.Exists(executable))
        {
            Log.Warn("Skipping file association command, missing: " + executable);
            return;
        }

        UserRegistry.SetString($@"Software\Classes\{progId}\shell\open\command", "", $"\"{executable}\" \"%1\"");
    }

    private static void Apply(string setUserFta, IReadOnlyList<(string Type, string ProgId)> associations)
    {
        if (associations.Count == 0 || TryApplyAll(setUserFta, associations))
            return;

        foreach ((string type, string progId) in associations)
        {
            try
            {
                ProcessRunner.Run(setUserFta, CommandLine.Quote(type) + " " + CommandLine.Quote(progId), TimeSpan.FromSeconds(30));
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or TimeoutException)
            {
                Log.Error($"SetUserFTA {type} failed", ex);
            }
        }
    }

    private static bool TryApplyAll(string setUserFta, IReadOnlyList<(string Type, string ProgId)> associations)
    {
        string listFile = Path.Combine(Path.GetTempPath(), $"SalsaNOW-fta-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllLines(listFile, associations.Select(association => $"{association.Type}, {association.ProgId}"));
            return ProcessRunner.Run(setUserFta, CommandLine.Quote(listFile), TimeSpan.FromMinutes(1)) == 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException or TimeoutException)
        {
            Log.Warn("SetUserFTA batch failed, applying one at a time: " + ex.Message);
            return false;
        }
        finally
        {
            FileSystem.TryDeleteFile(listFile);
        }
    }
}
