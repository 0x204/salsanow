namespace SalsaNOW.Core.Persistence;

public sealed class RegistryBackupList
{
    public int BackupIntervalSeconds { get; set; } = 5;

    public List<RegistryBackupEntry> RegistryKeys { get; set; } = [];
}

public sealed class RegistryBackupEntry
{
    public string Key { get; set; } = "";

    public string File { get; set; } = "";
}
