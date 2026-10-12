using SalsaNOW.Core.Shell;

namespace SalsaNOW.Tests.Shell;

public class CtrlWinGestureTests
{
    private const int KeyD = 0x44;
    private const int Left = 0x25;
    private const int Shift = 0xA0;

    private readonly CtrlWinGesture _gesture = new();

    [Fact]
    public void CompletesOnWinReleaseWhileControlIsHeld()
    {
        Assert.False(Press(CtrlWinGesture.LeftControl, control: false));
        Assert.False(Press(CtrlWinGesture.LeftWin, control: true));

        Assert.True(Release(CtrlWinGesture.LeftWin, control: true));
    }

    [Fact]
    public void CompletesWhenControlFollowsWin()
    {
        Press(CtrlWinGesture.RightWin, control: false);
        Press(CtrlWinGesture.RightControl, control: true);

        Assert.True(Release(CtrlWinGesture.RightWin, control: true));
    }

    [Fact]
    public void IgnoresWinAlone()
    {
        Press(CtrlWinGesture.LeftWin, control: false);

        Assert.False(Release(CtrlWinGesture.LeftWin, control: false));
    }

    [Theory]
    [InlineData(KeyD)]
    [InlineData(Left)]
    [InlineData(Shift)]
    public void IgnoresShortcutsThatUseAnotherKey(int otherKey)
    {
        Press(CtrlWinGesture.LeftControl, control: false);
        Press(CtrlWinGesture.LeftWin, control: true);
        Press(otherKey, control: true);
        Press(CtrlWinGesture.LeftWin, control: true);

        Assert.False(Release(CtrlWinGesture.LeftWin, control: true));
    }

    [Fact]
    public void StartsOverAfterEachWinRelease()
    {
        Press(CtrlWinGesture.LeftWin, control: true);
        Press(KeyD, control: true);
        Release(CtrlWinGesture.LeftWin, control: true);

        Press(CtrlWinGesture.LeftWin, control: true);

        Assert.True(Release(CtrlWinGesture.LeftWin, control: true));
    }

    private bool Press(int key, bool control) => _gesture.OnKey(key, down: true, controlHeld: control);

    private bool Release(int key, bool control) => _gesture.OnKey(key, down: false, controlHeld: control);
}
