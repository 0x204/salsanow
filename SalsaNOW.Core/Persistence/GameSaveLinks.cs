using SalsaNOW.Core.IO;

namespace SalsaNOW.Core.Persistence;

public sealed record GameSaveLink(string Folder, string Target);

public static class GameSaveLinks
{
    public static IReadOnlyList<GameSaveLink> Plan(IEnumerable<string> folders, string savesRoot, out IReadOnlyList<string> conflicts)
    {
        var links = new List<GameSaveLink>();
        var skipped = new List<string>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string folder in folders.Where(f => !string.IsNullOrWhiteSpace(f)))
        {
            string name = PathRules.LastSegment(folder);
            if (name.Length == 0 || name.EndsWith(":", StringComparison.Ordinal) || !usedNames.Add(name))
            {
                skipped.Add(folder);
                continue;
            }

            links.Add(new GameSaveLink(folder, savesRoot.TrimEnd('\\') + "\\" + name));
        }

        conflicts = skipped;
        return links;
    }
}
