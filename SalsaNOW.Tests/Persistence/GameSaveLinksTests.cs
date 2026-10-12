using SalsaNOW.Core.Persistence;

namespace SalsaNOW.Tests.Persistence;

public class GameSaveLinksTests
{
    private const string SavesRoot = @"I:\Apps\SalsaNOW\Game Saves";

    [Fact]
    public void MapsEachFolderByName()
    {
        string[] folders = [@"C:\Users\Public\Documents", @"C:\Program Files (x86)\Steam\steam\games"];

        IReadOnlyList<GameSaveLink> links = GameSaveLinks.Plan(folders, SavesRoot, out IReadOnlyList<string> conflicts);

        Assert.Empty(conflicts);
        GameSaveLink[] expected =
        [
            new(@"C:\Users\Public\Documents", @"I:\Apps\SalsaNOW\Game Saves\Documents"),
            new(@"C:\Program Files (x86)\Steam\steam\games", @"I:\Apps\SalsaNOW\Game Saves\games"),
        ];
        Assert.Equal(expected, links);
    }

    [Fact]
    public void SkipsFoldersWhoseNameIsTaken()
    {
        string[] folders = [@"C:\Users\kiosk\Documents\My Games", @"C:\Users\kiosk\Saved Games\My Games", @"C:\Users\Public\Documents"];

        IReadOnlyList<GameSaveLink> links = GameSaveLinks.Plan(folders, SavesRoot, out IReadOnlyList<string> conflicts);

        Assert.Equal(2, links.Count);
        Assert.Equal(@"C:\Users\kiosk\Saved Games\My Games", Assert.Single(conflicts));
    }

    [Fact]
    public void SkipsDriveRootsAndBlankEntries()
    {
        IReadOnlyList<GameSaveLink> links = GameSaveLinks.Plan([@"C:\", "", "  "], SavesRoot, out IReadOnlyList<string> conflicts);

        Assert.Empty(links);
        Assert.Equal(@"C:\", Assert.Single(conflicts));
    }
}
