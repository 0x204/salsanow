using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SalsaNOW
{
    internal class AutoPersist
    {
        public static async Task BackupDesktopRegistry(CancellationToken token, string globalDirectory)
        {
            const string jsonUrl = "https://salsanowfiles.work/ExplorerContents/jsons/RegistryList.json";

            string backupDir = Path.Combine(globalDirectory, "RegistryBackups");
            Directory.CreateDirectory(backupDir);

            using (WebClient client = new WebClient())
            {
                // Download config once
                string json = await client.DownloadStringTaskAsync(jsonUrl);

                RegistryBackupConfig config =
                    JsonConvert.DeserializeObject<RegistryBackupConfig>(json);

                if (config == null)
                    return;

                // Restore existing backups ONCE
                foreach (RegistryEntry entry in config.RegistryKeys)
                {
                    string file = Path.Combine(backupDir, entry.File);

                    if (!File.Exists(file))
                        continue;

                    try
                    {
                        SalsaLogger.Info("Restoring registry: " + entry.Key);

                        int exitCode = RunReg($"import \"{file}\"");
                        if (exitCode != 0)
                        {
                            File.Copy(file, file + ".failed", true);
                            SalsaLogger.Error($"Failed to restore {entry.Key} (reg.exe exit code {exitCode}). The backup was kept as {entry.File}.failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        SalsaLogger.Error("Failed to restore " + entry.Key + ": " + ex.Message);
                    }
                }

                SalsaLogger.Info("Initial registry restore complete.");

                // Backup loop
                var failingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (!token.IsCancellationRequested)
                {
                    foreach (RegistryEntry entry in config.RegistryKeys)
                        BackupKey(entry, backupDir, failingKeys);

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(config.BackupIntervalSeconds), token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private static void BackupKey(RegistryEntry entry, string backupDir, HashSet<string> failingKeys)
        {
            string file = Path.Combine(backupDir, entry.File);
            string temp = file + ".tmp";

            try
            {
                int exitCode = RunReg($"export \"{entry.Key}\" \"{temp}\" /y");
                if (exitCode != 0 || !File.Exists(temp))
                {
                    if (failingKeys.Add(entry.Key))
                        SalsaLogger.Warn($"Could not back up {entry.Key} (reg.exe exit code {exitCode}).");
                    return;
                }

                failingKeys.Remove(entry.Key);

                if (FilesEqual(temp, file))
                    return;

                if (File.Exists(file))
                    ReplaceFile(temp, file);
                else
                    File.Move(temp, file);
            }
            catch (Exception ex)
            {
                if (failingKeys.Add(entry.Key))
                    SalsaLogger.Error("Failed to back up " + entry.Key + ": " + ex.Message);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }

        private static void ReplaceFile(string source, string destination)
        {
            try
            {
                File.Replace(source, destination, null);
            }
            catch (IOException)
            {
                File.Copy(source, destination, true);
            }
        }

        private static bool FilesEqual(string left, string right)
        {
            if (!File.Exists(right) || new FileInfo(left).Length != new FileInfo(right).Length)
                return false;

            const int BufferSize = 81920;
            using (var a = File.OpenRead(left))
            using (var b = File.OpenRead(right))
            {
                var bufferA = new byte[BufferSize];
                var bufferB = new byte[BufferSize];
                int read;
                while ((read = a.Read(bufferA, 0, BufferSize)) > 0)
                {
                    int offset = 0;
                    while (offset < read)
                    {
                        int readB = b.Read(bufferB, offset, read - offset);
                        if (readB == 0)
                            return false;
                        offset += readB;
                    }

                    for (int i = 0; i < read; i++)
                    {
                        if (bufferA[i] != bufferB[i])
                            return false;
                    }
                }
            }

            return true;
        }

        private static int RunReg(string arguments)
        {
            using (Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }))
            {
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        public static async Task ApplyCustomRegistryFiles(string globalDirectory)
        {
            const string jsonUrl =
                "https://salsanowfiles.work/ExplorerContents/jsons/RegistryFiles.json";

            string downloadDir = Path.Combine(globalDirectory, "RegistryFiles");
            Directory.CreateDirectory(downloadDir);

            using (WebClient client = new WebClient())
            {
                string json = await client.DownloadStringTaskAsync(jsonUrl);

                var files =
                    JsonConvert.DeserializeObject<List<RegistryFile>>(json);

                if (files == null)
                    return;

                foreach (RegistryFile entry in files)
                {
                    string regFile = Path.Combine(
                        downloadDir,
                        Path.GetFileName(entry.File));

                    await client.DownloadFileTaskAsync(
                        entry.Url,
                        regFile);

                    int exitCode = RunReg("import \"" + regFile + "\"");
                    if (exitCode != 0)
                        SalsaLogger.Error($"Failed to import {entry.File} (reg.exe exit code {exitCode}).");
                }
            }
        }

        // Sets up directory junctions so that the given paths persist on gfn
        public static async Task SetupGameSavesAsync(string globalDirectory)
        {
            try
            {
                SalsaLogger.Info("Setting up Cloud Save directory junctions...");
                string json;
                using (var wc = new WebClient()) json = await wc.DownloadStringTaskAsync("https://salsanowfiles.work/jsons/GameSavesPaths.json");
                var savePaths = JsonConvert.DeserializeObject<GamesSavePaths>(json);
                string savesRoot = Path.Combine(globalDirectory, "Game Saves");
                Directory.CreateDirectory(savesRoot);

                foreach (var dir in savePaths.paths)
                {
                    string crafted = Path.Combine(savesRoot, Path.GetFileName(dir));
                    Directory.CreateDirectory(crafted);

                    if (IsSteamGamesFolder(dir))
                        PersistExistingFolderContents(dir, crafted);

                    string parent = Path.GetDirectoryName(dir);
                    if (!string.IsNullOrEmpty(parent))
                        Directory.CreateDirectory(parent);

                    RunHiddenCmd($"/c rmdir /s /q \"{dir}\"");
                    RunHiddenCmd($"/c mklink /J \"{dir}\" \"{crafted}\"");

                    if (dir.Contains(@"C:\Users\Public\Documents")) await HandlePublicDocs(dir, crafted);
                }
                SalsaLogger.Info("Cloud Save junctions successfully created.");
            }
            catch (Exception ex) { SalsaLogger.Error($"Game Saves Setup Error: {ex.Message}"); }
        }

        private static async Task HandlePublicDocs(string dir, string crafted)
        {
            // Kill NvContainerWindowClass to release the lock on Public Documents
            foreach (var p in Process.GetProcessesByName("NVDisplay.Container"))
            {
                NativeMethods.EnumWindows((hWnd, lp) => {
                    NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == p.Id)
                    {
                        var sb = new StringBuilder(256);
                        NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
                        if (sb.ToString().StartsWith("NvContainerWindowClass", StringComparison.OrdinalIgnoreCase))
                            NativeMethods.PostMessage(hWnd, (uint)NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                    return true;
                }, IntPtr.Zero);
            }

            await Task.Delay(150);

            // Retry loop to ensure junction is created once the process releases the handle
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                    if (!Directory.Exists(dir)) { RunHiddenCmd($"/c mklink /J \"{dir}\" \"{crafted}\""); break; }
                }
                catch { }
                await Task.Delay(150);
            }
        }

        private static bool IsSteamGamesFolder(string dir)
        {
            try
            {
                return string.Equals(
                    Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Path.GetFullPath(@"C:\Program Files (x86)\Steam\steam\games"),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void PersistExistingFolderContents(string source, string dest)
        {
            if (!Directory.Exists(source))
                return;
            if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                return;

            try
            {
                string sourceRoot = source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string target = Path.Combine(dest, relative);
                    string targetParent = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(targetParent))
                        Directory.CreateDirectory(targetParent);
                    if (!File.Exists(target))
                        File.Copy(file, target);
                }
                SalsaLogger.Info("Persisted existing Steam steam\\games folder contents.");
            }
            catch (Exception ex)
            {
                SalsaLogger.Error("Failed to persist Steam steam\\games contents: " + ex.Message);
            }
        }

        private static void RunHiddenCmd(string args)
        {
            Process.Start(new ProcessStartInfo("cmd.exe", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit();
        }
    }
}
