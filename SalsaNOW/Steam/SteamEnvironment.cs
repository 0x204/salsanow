using System.Collections;
using SalsaNOW.Core.Steam;

namespace SalsaNOW.Steam;

internal static class SteamEnvironment
{
    public static void Clear()
    {
        List<string> names = Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Select(variable => (string)variable.Key)
            .Where(SteamEnvironmentVariables.IsSteamVariable)
            .ToList();

        foreach (string name in names)
            Environment.SetEnvironmentVariable(name, null);
    }
}
