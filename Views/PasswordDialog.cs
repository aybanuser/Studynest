using StudyNest.Services;

namespace StudyNest.Views;

/// <summary>
/// Small popups that ask for a password. Pure UI: they only collect text.
/// Checking the password is LockService's job. Both methods return null if the user cancels.
/// </summary>
internal static class PasswordDialog
{
    /// <summary>Asks for a NEW password, typed twice, and validates it before closing.</summary>
    public static string? AskNew(IWin32Window owner, string title)
    {
        using var dialog = CreateDialog(title, 330);

        // The controls: two masked boxes, an error line, and two buttons.
        var first = new TextBox { UseSystemPasswordChar = true, Location = new Point(18, 32), Width = 305 };
        var second = new TextBox { UseSystemPasswordChar = true, Location = new Point(18, 82), Width = 305 };
        var error = new Label { ForeColor = Color.Firebrick, Location = new Point(18, 112), AutoSize = true };

        // "Lock" has NO DialogResult on purpose: setting it would close the popup before we validate.
        var ok = new Button { Text = "Lock", Location = new Point(165, 205), Width = 75 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(248, 205), Width = 75 };

        dialog.Controls.Add(new Label { Text = $"Choose a password (at least {LockService.MinPasswordLength} characters):", Location = new Point(18, 12), AutoSize = true });
        dialog.Controls.Add(first);
        dialog.Controls.Add(new Label { Text = "Type it again:", Location = new Point(18, 62), AutoSize = true });
        dialog.Controls.Add(second);
        dialog.Controls.Add(error);
        dialog.Controls.Add(new Label { Text = "If you forget it, it cannot be recovered.", ForeColor = Theme.Muted, Location = new Point(18, 150), AutoSize = true });
        dialog.Controls.Add(ok);
        dialog.Controls.Add(cancel);
        dialog.AcceptButton = ok;    // Enter key = Lock
        dialog.CancelButton = cancel; // Esc key = Cancel
        dialog.ActiveControl = first; // cursor starts in the first box

        // Validate first. Only a valid password closes the popup (by setting DialogResult.OK).
        ok.Click += (_, _) =>
        {
            if (first.Text.Length < LockService.MinPasswordLength)
            {
                error.Text = $"Use at least {LockService.MinPasswordLength} characters.";
                return;
            }
            if (first.Text != second.Text)
            {
                error.Text = "The passwords don't match.";
                return;
            }
            dialog.DialogResult = DialogResult.OK;
        };

        return dialog.ShowDialog(owner) == DialogResult.OK ? first.Text : null;
    }

    /// <summary>Asks for an existing password (one masked box). Used by "Remove lock".</summary>
    public static string? Ask(IWin32Window owner, string title, string message)
    {
        using var dialog = CreateDialog(title, 160);
        var input = new TextBox { UseSystemPasswordChar = true, Location = new Point(18, 32), Width = 305 };
        // Here the buttons DO set DialogResult: there is nothing to validate in the popup itself.
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(165, 72), Width = 75 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(248, 72), Width = 75 };

        dialog.Controls.Add(new Label { Text = message, Location = new Point(18, 12), AutoSize = true });
        dialog.Controls.Add(input);
        dialog.Controls.Add(ok);
        dialog.Controls.Add(cancel);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        dialog.ActiveControl = input;

        return dialog.ShowDialog(owner) == DialogResult.OK ? input.Text : null;
    }

    /// <summary>Makes the empty popup window both dialogs share (fixed size, centered on the main window).</summary>
    private static Form CreateDialog(string title, int height)
    {
        return new Form
        {
            Text = title,
            Size = new Size(360, height),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false
        };
    }
}