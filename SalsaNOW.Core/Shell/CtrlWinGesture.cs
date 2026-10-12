namespace SalsaNOW.Core.Shell;

public sealed class CtrlWinGesture
{
    public const int Control = 0x11;
    public const int LeftWin = 0x5B;
    public const int RightWin = 0x5C;
    public const int LeftControl = 0xA2;
    public const int RightControl = 0xA3;

    private bool _winHeld;
    private bool _withControl;
    private bool _withOtherKey;

    public bool OnKey(int virtualKey, bool down, bool controlHeld)
    {
        if (virtualKey is LeftWin or RightWin)
        {
            if (down)
            {
                if (!_winHeld)
                {
                    _winHeld = true;
                    _withControl = controlHeld;
                    _withOtherKey = false;
                }

                return false;
            }

            bool completed = _winHeld && _withControl && !_withOtherKey;
            _winHeld = false;
            return completed;
        }

        if (_winHeld && down)
        {
            if (virtualKey is Control or LeftControl or RightControl)
                _withControl = true;
            else
                _withOtherKey = true;
        }

        return false;
    }
}
