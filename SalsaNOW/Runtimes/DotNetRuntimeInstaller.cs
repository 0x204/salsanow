using System.IO.Compression;
using SalsaNOW.Core.DotNet;
using SalsaNOW.Core.IO;
using SalsaNOW.Infrastructure;
using SalsaNOW.Net;

namespace SalsaNOW.Runtimes;

internal static class DotNetRuntimeInstaller
{
    private static readonly string[] Channels = ["5.0", "6.0", "7.0", "8.0", "9.0", "10.0"];

    private const string FinalDotNet5Version = "5.0.17";

    private const string CompleteMarker = ".salsanow-complete";

    public static string InstallDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "dotnet");

    public static async Task InstallAsync(CancellationToken cancellationToken)
    {
        Log.Info("Starting .NET runtime install...");
        Directory.CreateDirectory(InstallDirectory);

        string[] missing = Channels.Where(channel => !IsInstalled(channel)).ToArray();
        if (missing.Length == 0)
        {
            Log.Info("All required .NET runtimes are already installed.");
            return;
        }

        RuntimePack[][] packsByChannel = await Task.WhenAll(missing.Select(channel => ResolvePacksAsync(channel, cancellationToken)));
        RuntimePack[] packs = packsByChannel.SelectMany(channelPacks => channelPacks).ToArray();
        if (packs.Length == 0)
            return;

        try
        {
            Log.Info("Downloading all .NET zips...");
            await Task.WhenAll(packs.Select(pack => pack.DownloadAsync(cancellationToken)));

            var brokenChannels = new HashSet<string>(packs.Where(pack => !pack.IsDownloaded).Select(pack => pack.Channel));
            if (brokenChannels.Count > 0)
                Log.Error("Some .NET zips are missing or damaged. Skipping .NET " + string.Join(", ", brokenChannels));

            Log.Info("Extracting .NET runtimes to " + InstallDirectory);
            foreach (RuntimePack pack in packs.Where(pack => !brokenChannels.Contains(pack.Channel)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (Extract(pack.ArchivePath!, InstallDirectory, out IReadOnlyCollection<string> frameworkFolders))
                    {
                        foreach (string folder in frameworkFolders)
                            File.WriteAllText(Path.Combine(folder, CompleteMarker), pack.Label);

                        Log.Info("Extracted " + pack.Label);
                    }
                    else
                    {
                        Log.Error($"{pack.Label} was only partly extracted. It will be extracted again next session.");
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
                {
                    Log.Error("Failed to extract " + pack.Label, ex);
                }
            }
        }
        finally
        {
            foreach (RuntimePack pack in packs)
                pack.DeleteArchive();
        }

        string[] stillMissing = Channels.Where(channel => !IsInstalled(channel)).ToArray();
        if (stillMissing.Length == 0)
            Log.Info("Successfully installed all .NET runtimes.");
        else
            Log.Error("DotNet install finished with missing runtimes: " + string.Join(", ", stillMissing));
    }

    private static bool IsInstalled(string channel)
    {
        return HasSharedFramework("Microsoft.NETCore.App", channel)
            && HasSharedFramework("Microsoft.WindowsDesktop.App", channel)
            && HasSharedFramework("Microsoft.AspNetCore.App", channel);
    }

    private static bool HasSharedFramework(string framework, string channel)
    {
        string frameworkDirectory = Path.Combine(InstallDirectory, "shared", framework);
        return Directory.Exists(frameworkDirectory)
            && Directory.GetDirectories(frameworkDirectory)
                .Where(version => RuntimeRelease.IsChannelFolder(Path.GetFileName(version), channel))
                .Any(version => File.Exists(Path.Combine(version, CompleteMarker)));
    }

    private static async Task<RuntimePack[]> ResolvePacksAsync(string channel, CancellationToken cancellationToken)
    {
        string? version = channel == "5.0" ? FinalDotNet5Version : await GetLatestVersionAsync(channel, cancellationToken);
        if (string.IsNullOrEmpty(version))
        {
            Log.Error($"Could not resolve latest .NET {channel} version.");
            return [];
        }

        return
        [
            new RuntimePack(channel, version!, "WindowsDesktop", "windowsdesktop-runtime-win-x64.zip", "WindowsDesktop", $"windowsdesktop-runtime-{version}-win-x64.zip"),
            new RuntimePack(channel, version!, "ASP.NET", "aspnetcore-runtime-win-x64.zip", "aspnetcore/Runtime", $"aspnetcore-runtime-{version}-win-x64.zip"),
        ];
    }

    private static async Task<string?> GetLatestVersionAsync(string channel, CancellationToken cancellationToken)
    {
        string[] endpoints =
        [
            $"https://builds.dotnet.microsoft.com/dotnet/Runtime/{channel}/latest.version",
            $"https://dotnetcli.azureedge.net/dotnet/Runtime/{channel}/latest.version",
        ];

        foreach (string endpoint in endpoints)
        {
            try
            {
                string? version = RuntimeRelease.ParseLatestVersion(await Http.GetStringAsync(endpoint, cancellationToken));
                if (!string.IsNullOrEmpty(version))
                    return version;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
            }
        }

        return null;
    }

    private static bool Extract(string archivePath, string destination, out IReadOnlyCollection<string> frameworkFolders)
    {
        string root = Path.GetFullPath(destination).TrimEnd('\\');
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        frameworkFolders = folders;
        bool complete = true;

        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (!ZipEntryPath.TryResolve(root, entry.FullName, entry.ExternalAttributes, out string relativePath, out bool isDirectory))
                continue;

            string target = Path.Combine(root, relativePath);
            try
            {
                if (isDirectory)
                {
                    Directory.CreateDirectory(target);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                FileSystem.DeleteFile(target);
                entry.ExtractToFile(target);

                string[] segments = relativePath.Split('\\');
                if (segments.Length > 3 && string.Equals(segments[0], "shared", StringComparison.OrdinalIgnoreCase))
                    folders.Add(Path.Combine(root, segments[0], segments[1], segments[2]));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                complete = false;
                Log.Warn($"Skipped zip entry {entry.FullName}: {ex.Message}");
            }
        }

        return complete;
    }

    private sealed class RuntimePack(string channel, string version, string name, string akaFileName, string cdnFolder, string cdnFileName)
    {
        public string Channel { get; } = channel;

        public string Label { get; } = $".NET {channel} {name}";

        public string? ArchivePath { get; private set; }

        public bool IsDownloaded => ArchivePath is not null && IsIntactZip(ArchivePath);

        public async Task DownloadAsync(CancellationToken cancellationToken)
        {
            Log.Info($"Downloading {Label}...");
            string path = Path.Combine(Path.GetTempPath(), $"salsanow-dotnet-{Guid.NewGuid():N}.zip");
            Exception? lastError = null;

            foreach (string url in Urls())
            {
                try
                {
                    await Http.DownloadFileAsync(url, path, cancellationToken);
                    if (IsIntactZip(path))
                    {
                        ArchivePath = path;
                        return;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    lastError = ex;
                }

                FileSystem.TryDeleteFile(path);
            }

            Log.Error($"Download failed for {Label}: {lastError?.Message ?? "no mirror returned a valid zip."}");
        }

        public void DeleteArchive()
        {
            if (ArchivePath is not null)
                FileSystem.TryDeleteFile(ArchivePath);
        }

        private IEnumerable<string> Urls()
        {
            if (Channel != "5.0")
                yield return $"https://aka.ms/dotnet/{Channel}/{akaFileName}";

            yield return $"https://builds.dotnet.microsoft.com/dotnet/{cdnFolder}/{version}/{cdnFileName}";
            yield return $"https://dotnetcli.azureedge.net/dotnet/{cdnFolder}/{version}/{cdnFileName}";
        }

        private static bool IsIntactZip(string path)
        {
            try
            {
                using ZipArchive archive = ZipFile.OpenRead(path);
                return archive.Entries.Count > 0;
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
