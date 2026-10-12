using System.Windows.Forms;

namespace SalsaNOW.Infrastructure;

internal static class Dialogs
{
    public static void ShowError(string text, string caption)
    {
        RunOnStaThread(() => MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error)).Join();
    }

    public static Task ShowInfoAsync(string text, string caption)
    {
        var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        RunOnStaThread(() =>
        {
            try
            {
                MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                closed.TrySetResult(true);
            }
        });

        return closed.Task;
    }

    public static Thread RunOnStaThread(Action action)
    {
        var thread = new Thread(() => action()) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return thread;
    }
}
