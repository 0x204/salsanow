using Microsoft.Win32;

namespace SalsaNOW.Infrastructure;

internal static class UserRegistry
{
    public static void SetDword(string keyPath, string name, int value) => Set(keyPath, name, value, RegistryValueKind.DWord);

    public static void SetBinary(string keyPath, string name, byte[] value) => Set(keyPath, name, value, RegistryValueKind.Binary);

    public static void SetString(string keyPath, string name, string value) => Set(keyPath, name, value, RegistryValueKind.String);

    public static bool DeleteTree(string keyPath)
    {
        using (RegistryKey? existing = Registry.CurrentUser.OpenSubKey(keyPath))
        {
            if (existing is null)
                return false;
        }

        Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
        return true;
    }

    private static void Set(string keyPath, string name, object value, RegistryValueKind kind)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath)
            ?? throw new UnauthorizedAccessException($@"Could not open HKCU\{keyPath}.");
        key.SetValue(name, value, kind);
    }
}
