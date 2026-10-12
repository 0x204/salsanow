using SalsaNOW.Core.Json;
using SalsaNOW.Core.Settings;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Configuration;

internal sealed class UserSettingsFile(string path)
{
    private string? _lastError;

    public string FilePath { get; } = path;

    public bool Exists => File.Exists(FilePath);

    public DateTime LastWriteTimeUtc => File.GetLastWriteTimeUtc(FilePath);

    public UserSettings? TryRead()
    {
        if (!Exists)
            return null;

        try
        {
            UserSettings settings = JsonConfig.Parse<UserSettings>(File.ReadAllText(FilePath));
            _lastError = null;
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            if (_lastError != ex.Message)
            {
                _lastError = ex.Message;
                Log.Error("SalsaNOWSettings.json failed", ex);
            }

            return null;
        }
    }
}
