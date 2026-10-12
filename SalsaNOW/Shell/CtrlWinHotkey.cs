using System.ComponentModel;
using System.Runtime.InteropServices;
using SalsaNOW.Core.Shell;
using SalsaNOW.Infrastructure;
using static SalsaNOW.Infrastructure.NativeMethods;

namespace SalsaNOW.Shell;

internal sealed class CtrlWinHotkey
{
    public const string DefaultApp = "WindowsMenuToggle";

    private readonly string _program;
    private readonly LowLevelKeyboardProc _callback;
    private readonly CtrlWinGesture _gesture = new();
    private readonly TaskCompletionSource<bool> _hooked = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private CtrlWinHotkey(string program)
    {
        _program = program;
        _callback = OnKeyboardEvent;
    }

    public static Task<bool> StartAsync(string program)
    {
        var hotkey = new CtrlWinHotkey(Environment.ExpandEnvironmentVariables(program.Trim()));
        new Thread(hotkey.Run) { IsBackground = true, Name = "Ctrl+Win hotkey" }.Start();
        return hotkey._hooked.Task;
    }

    private void Run()
    {
        IntPtr hook = SetWindowsHookEx(WH_KEYBOARD_LL, _callback, GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero)
        {
            var error = new Win32Exception();
            _hooked.SetResult(false);
            Log.Error("Could not watch Ctrl+Win", error);
            return;
        }

        _hooked.SetResult(true);
        Log.Info($"Ctrl+Win opens {_program}.");
        try
        {
            while (GetMessage(out MSG message, IntPtr.Zero, 0, 0) > 0)
                DispatchMessage(ref message);
        }
        finally
        {
            UnhookWindowsHookEx(hook);
        }
    }

    private IntPtr OnKeyboardEvent(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            int virtualKey = (int)Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam).vkCode;
            int message = wParam.ToInt32();
            bool down = message is WM_KEYDOWN or WM_SYSKEYDOWN;
            bool repeat = down && GetAsyncKeyState(virtualKey) < 0;
            bool controlHeld = GetAsyncKeyState(VK_CONTROL) < 0;
            if (_gesture.OnKey(virtualKey, down, repeat, controlHeld))
                ThreadPool.QueueUserWorkItem(_ => Launch());
        }

        return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
    }

    private void Launch()
    {
        try
        {
            string? directory = Path.IsPathRooted(_program) ? Path.GetDirectoryName(_program) : null;
            ProcessRunner.Launch(_program, workingDirectory: directory)?.Dispose();
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or IOException)
        {
            Log.Error($"Could not open {_program}", ex);
        }
    }
}
