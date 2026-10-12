using System.Drawing;
using System.Windows.Forms;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Desktop;

internal static class WelcomeNotice
{
    private const string Title = "SalsaNOW";
    private const string Instruction = "Always close the session from the Windows Start menu button \"Shutdown Session\".";
    private const string Content =
        "The following paths persist if you have persistent storage:\n" +
        "• C:\\Users\\kiosk\\Documents\n" +
        "• C:\\Users\\kiosk\\AppData (all AppData)\n" +
        "• C:\\Users\\public\\Documents\n" +
        "• I:\\ (the whole drive persists)\n\n" +
        "ANTICHEAT GAMES ARE NOT SUPPORTED\n" +
        "SOFTWARE THAT REQUIRES ADMIN RIGHTS AT ALL TIMES IS NOT SUPPORTED";

    public static void ShowOnce(string marker)
    {
        if (File.Exists(marker))
            return;

        Dialogs.RunOnStaThread(() =>
        {
            try
            {
                Application.EnableVisualStyles();
                using (Form form = CreateForm())
                    form.ShowDialog();

                File.WriteAllText(marker, DateTime.Now.ToString("o"));
                Log.Info("First-run notice acknowledged.");
            }
            catch (Exception ex)
            {
                Log.Error("First-run notice failed", ex);
            }
        }).Join();
    }

    private static Form CreateForm()
    {
        var form = new Form
        {
            Text = Title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowIcon = false,
            ShowInTaskbar = false,
            TopMost = true,
            Font = new Font("Segoe UI", 9.75f),
            BackColor = Color.White,
            ClientSize = new Size(460, 280),
        };

        var button = new Button
        {
            Text = "I understand",
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.System,
            Size = new Size(112, 32),
        };

        var footer = new Panel { Height = 52, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(243, 243, 243) };
        footer.Controls.Add(button);
        footer.Layout += (_, _) => button.Location = new Point(
            footer.ClientSize.Width - button.Width - 20,
            (footer.ClientSize.Height - button.Height) / 2);

        var separator = new Panel { Height = 1, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(229, 229, 229) };

        var icon = new PictureBox
        {
            Image = SystemIcons.Warning.ToBitmap(),
            Size = new Size(32, 32),
            Location = new Point(20, 20),
            SizeMode = PictureBoxSizeMode.StretchImage,
        };

        var instruction = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(360, 0),
            Location = new Point(64, 18),
            Font = new Font("Segoe UI", 12f),
            Text = Instruction,
        };

        var content = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(360, 0),
            Font = new Font("Segoe UI", 9.75f),
            Text = Content,
        };

        var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        body.Controls.AddRange([icon, instruction, content]);

        form.Controls.AddRange([body, separator, footer]);
        form.AcceptButton = button;

        form.Load += (_, _) =>
        {
            content.Location = new Point(64, instruction.Bottom + 10);
            int bottom = Math.Max(icon.Bottom, content.Bottom) + 20;
            form.ClientSize = new Size(form.ClientSize.Width, bottom + separator.Height + footer.Height);
            WindowTools.RoundCorners(form.Handle);
        };

        form.FormClosed += (_, _) => icon.Image?.Dispose();
        return form;
    }
}
