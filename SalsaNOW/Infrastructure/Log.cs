namespace SalsaNOW.Infrastructure;

internal static class Log
{
    private static readonly object Sync = new();
    private static string? _filePath;

    public static void Initialize(string directory)
    {
        string path = Path.Combine(directory, "SalsaNOW.log");
        lock (Sync)
        {
            try
            {
                if (File.Exists(path))
                    File.Copy(path, Path.Combine(directory, "SalsaNOW.previous.log"), overwrite: true);

                File.WriteAllText(path, $"--- SalsaNOW {AppInfo.Version} session log [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ---{Environment.NewLine}");
                _filePath = path;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                WriteToConsole("[!]", "Could not create the log file: " + ex.Message, ConsoleColor.Yellow);
            }
        }
    }

    public static void Info(string message) => Write("[+]", message, ConsoleColor.Gray);

    public static void Warn(string message) => Write("[!]", message, ConsoleColor.Yellow);

    public static void Error(string message) => Write("[ERR]", message, ConsoleColor.Red);

    public static void Error(string message, Exception exception) => Error($"{message}: {exception.Message}");

    public static string ReadAll()
    {
        lock (Sync)
        {
            try
            {
                return _filePath is not null && File.Exists(_filePath) ? File.ReadAllText(_filePath) : "";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return "";
            }
        }
    }

    private static void Write(string prefix, string message, ConsoleColor color)
    {
        lock (Sync)
        {
            WriteToConsole(prefix, message, color);

            if (_filePath is null)
                return;

            try
            {
                File.AppendAllText(_filePath, $"[{DateTime.Now:HH:mm:ss}] {prefix} {message}{Environment.NewLine}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static void WriteToConsole(string prefix, string message, ConsoleColor color)
    {
        ConsoleColor original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"{prefix} {message}");
        Console.ForegroundColor = original;
    }
}
