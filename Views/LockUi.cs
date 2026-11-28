using StudyNest.Models;
using StudyNest.Services;

namespace StudyNest.Views;

/// <summary>
/// Builds the lock-related buttons and screens so StudyNestForm stays small.
/// This class only draws and collects input. LockService makes every decision.
/// (locks and owner come from the primary constructor and are usable in every method.)
/// </summary>
internal sealed class LockUi(LockService locks, Form owner)
{
    /// <summary>
    /// A toolbar-style button that opens a small menu.
    /// No password yet: "Set password...". Has a password: "Lock now" and "Remove lock...".
    /// Used for both pages and notebooks, because both are ILockable.
    /// </summary>
    /// <param name="label">Button text, e.g. "Page lock".</param>
    /// <param name="itemName">Used in popup text, e.g. "this page".</param>
    /// <param name="refresh">Called after any change so the screen redraws.</param>
    public Button CreateMenuButton(string label, string itemName, ILockable item, Action refresh)
    {
        var hasLock = locks.HasLock(item);

        // Same look as the other toolbar buttons. The 🔒 shows that a password exists.
        var button = new Button
        {
            Text = (hasLock ? "🔒 " : "") + label + " ▾",
            AutoSize = true,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Theme.Ink,
            Font = new Font("Segoe UI", 9),
            Padding = new Padding(8, 0, 8, 0)
        };
        button.FlatAppearance.BorderColor = Theme.Border;
        button.FlatAppearance.MouseOverBackColor = Theme.SoftAccent;

        // The menu's items depend on the state when the screen was built.
        // That is fine because every action calls refresh(), which rebuilds the screen.
        var menu = new ContextMenuStrip();
        if (!hasLock)
        {
            menu.Items.Add("Set password...", null, (_, _) => SetPassword(item, refresh));
        }
        else
        {
            menu.Items.Add("Lock now", null, (_, _) => { locks.LockNow(item); refresh(); });
            menu.Items.Add("Remove lock...", null, (_, _) => RemoveLock(item, itemName, refresh));
        }

        button.Click += (_, _) => menu.Show(button, new Point(0, button.Height)); // open just below the button
        button.Disposed += (_, _) => menu.Dispose(); // free the menu with the button
        return button;
    }

    /// <summary>
    /// Adds the "This page is locked" screen (password box + Unlock button) to a panel.
    /// Nothing from the locked item is shown here, so no content can leak.
    /// </summary>
    /// <param name="kind">"page" or "notebook" (used in the heading).</param>
    /// <param name="top">Y position where the screen starts.</param>
    /// <param name="onUnlocked">Called after a correct password (the form redraws itself).</param>
    public void AddLockedScreen(Control parent, string kind, string itemName, ILockable item, int top, Action onUnlocked)
    {
        // Heading and instruction text.
        parent.Controls.Add(new Label { Text = $"This {kind} is locked", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Theme.Ink, Location = new Point(34, top), AutoSize = true });
        parent.Controls.Add(new Label { Text = $"Enter the password to open \"{itemName}\".", ForeColor = Theme.Muted, Location = new Point(36, top + 58), AutoSize = true });

        // Password box, Unlock button, and a line for the "wrong password" message.
        var box = new TextBox { UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11), Width = 260, Location = new Point(36, top + 96) };
        var unlock = new Button { Text = "Unlock", FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Size = new Size(100, 30), Location = new Point(312, top + 94) };
        unlock.FlatAppearance.BorderSize = 0;
        unlock.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
        var error = new Label { ForeColor = Color.Firebrick, Location = new Point(36, top + 134), AutoSize = true };

        // Shared by the button click and the Enter key (a "local function").
        void TryUnlock()
        {
            if (locks.TryUnlock(item, box.Text))
            {
                onUnlocked(); // right password: let the form redraw the real screen
                return;
            }
            error.Text = "Wrong password. Try again.";
            box.SelectAll(); // so the next attempt replaces the old text
            box.Focus();
        }

        unlock.Click += (_, _) => TryUnlock();
        box.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true; // stops the "ding" sound
            TryUnlock();
        };

        parent.Controls.Add(box);
        parent.Controls.Add(unlock);
        parent.Controls.Add(error);
    }

    /// <summary>Menu action: ask for a new password, then save it through LockService.</summary>
    private void SetPassword(ILockable item, Action refresh)
    {
        var password = PasswordDialog.AskNew(owner, "Set password");
        if (password is null) return; // user cancelled
        locks.SetPassword(item, password);
        refresh();
    }

    /// <summary>Menu action: ask for the current password; remove the lock only if it is correct.</summary>
    private void RemoveLock(ILockable item, string itemName, Action refresh)
    {
        var password = PasswordDialog.Ask(owner, "Remove lock", $"Enter the password to remove the lock from {itemName}:");
        if (password is null) return; // user cancelled
        if (!locks.RemoveLock(item, password))
        {
            MessageBox.Show(owner, "Wrong password. The lock was not removed.", "StudyNest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        refresh();
    }
}