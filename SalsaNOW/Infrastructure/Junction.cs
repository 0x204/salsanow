namespace SalsaNOW.Infrastructure;

internal static class Junction
{
    public static bool TryReplaceDirectory(string folder, string target)
    {
        if (Directory.Exists(folder) && FileSystem.IsLink(folder) && FileSystem.IsSameDirectory(folder, target))
            return true;

        try
        {
            FileSystem.DeleteDirectory(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        bool linked = false;
        try
        {
            linked = ProcessRunner.Run("cmd.exe", $"/c mklink /J \"{folder}\" \"{target}\"", TimeSpan.FromSeconds(30)) == 0;
            return linked;
        }
        finally
        {
            if (!linked && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }
    }
}
