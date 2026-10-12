namespace SalsaNOW.Infrastructure;

internal static class Junction
{
    public static bool TryReplaceDirectory(string folder, string target)
    {
        try
        {
            FileSystem.DeleteDirectory(folder);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        if (ProcessRunner.Run("cmd.exe", $"/c mklink /J \"{folder}\" \"{target}\"", TimeSpan.FromSeconds(30)) == 0)
            return true;

        Directory.CreateDirectory(folder);
        return false;
    }
}
