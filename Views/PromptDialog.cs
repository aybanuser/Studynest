namespace StudyNest.Views;

/// <summary>A small "type a name" popup.</summary>
internal static class PromptDialog
{
    public static string Show(string message, string defaultValue)
    {
        using var dialog = new Form { Text = "New notebook", Size = new Size(360, 150), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false };
        var input = new TextBox { Text = defaultValue, Location = new Point(18, 30), Width = 305 };
        var ok = new Button { Text = "Create", DialogResult = DialogResult.OK, Location = new Point(165, 70), Width = 75 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(248, 70), Width = 75 };
        dialog.Controls.Add(new Label { Text = message, Location = new Point(18, 10), AutoSize = true });
        dialog.Controls.Add(input);
        dialog.Controls.Add(ok);
        dialog.Controls.Add(cancel);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;
        return dialog.ShowDialog() == DialogResult.OK ? input.Text : string.Empty;
    }
}
