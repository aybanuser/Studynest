using StudyNest.Models;
using StudyNest.Services;

namespace StudyNest.Views;

internal sealed class StudyNestForm : Form
{
    private readonly NotebookService service = new();
    // LOCK: locks = the locking rules. lockUi = builds the lock buttons and unlock screens.
    private readonly LockService locks = new();
    private readonly LockUi lockUi;

    private Notebook selectedNotebook;
    private PageNote selectedPage;
    private Panel content = null!;
    private Panel sidebar = null!;
    private FlowLayoutPanel notebookList = null!;
    private Button addNotebookButton = null!;
    private Label notebookTitle = null!;
    private Label pageTitle = null!;
    private TextBox pageEditor = null!;
    private System.Windows.Forms.Timer sidebarTimer = null!;

    public StudyNestForm()
    {
        selectedNotebook = service.Notebooks[0];
        selectedPage = selectedNotebook.Pages[0];
        Text = "StudyNest";
        MinimumSize = new Size(1080, 700);
        Size = new Size(1280, 800);
        BackColor = Color.White;
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true; // Enable form to capture key events
        KeyDown += StudyNestForm_KeyDown;
        lockUi = new LockUi(locks, this); // LOCK: must be created before BuildShell/ShowPage use it
        BuildShell();
        ShowPage();
    }

    private void StudyNestForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.S)
        {
            SaveCurrentPage();
            e.Handled = true; // Mark as handled to prevent further processing
        }
    }

    private void SaveCurrentPage()
    {
        // Data is bound to selectedPage.Content (set on TextChanged), 
        // so no explicit service call needed.
        MessageBox.Show("Page saved!", "StudyNest", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BuildShell()
    {
        var top = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.White };
        var toggleSidebar = new Button { Text = "☰", Font = new Font("Segoe UI", 12), FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Theme.Ink, Location = new Point(18, 17), Size = new Size(34, 34), AccessibleName = "Toggle sidebar" };
        toggleSidebar.FlatAppearance.BorderSize = 0;
        sidebarTimer = new System.Windows.Forms.Timer { Interval = 10 };
        sidebarTimer.Tick += (s, e) => {
            if (sidebar.Width < 244) {
                sidebar.Width += 20;
                if (sidebar.Width > 244) sidebar.Width = 244;
            } else {
                sidebarTimer.Stop();
            }
        };
        toggleSidebar.Click += (_, _) => {
            if (sidebar.Width > 0) sidebar.Width = 0;
            else sidebarTimer.Start();
        };
        top.Controls.Add(toggleSidebar);
        top.Controls.Add(new Label { Text = "StudyNest", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Theme.Ink, Location = new Point(68, 11), AutoSize = true });
        top.Controls.Add(new Label { Text = "Your notes, in one place.", Font = new Font("Segoe UI", 9), ForeColor = Theme.Muted, Location = new Point(211, 25), AutoSize = true });
        var search = new TextBox { PlaceholderText = "Search notebooks and pages...", BorderStyle = BorderStyle.FixedSingle, Width = 270, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(Width - 390, 18) };
        search.TextChanged += (_, _) => Search(search.Text);
        top.Controls.Add(search);
        var avatar = new Label { Text = "A", TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(238, 241, 255), ForeColor = Theme.Accent, Font = new Font("Segoe UI", 10, FontStyle.Bold), Size = new Size(34, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(Width - 78, 17) };
        top.Controls.Add(avatar);
        Controls.Add(top);

        sidebar = new Panel { Dock = DockStyle.Left, Width = 0, BackColor = Color.FromArgb(249, 250, 252), Padding = new Padding(18, 18, 12, 18) };
        sidebar.Controls.Add(new Label { Text = "WORKSPACE", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Theme.Muted, Location = new Point(20, 20), AutoSize = true });
        sidebar.Controls.Add(NavButton("All notes", 52, (_, _) => ShowPage()));
        sidebar.Controls.Add(NavButton("Study List", 91, (_, _) => ShowStudyList()));
        sidebar.Controls.Add(NavButton("Trash", 130, (_, _) => ShowTrash()));
        sidebar.Controls.Add(new Label { Text = "NOTEBOOKS", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Theme.Muted, Location = new Point(20, 188), AutoSize = true });
        notebookList = new FlowLayoutPanel { Location = new Point(12, 214), Size = new Size(214, 235), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        sidebar.Controls.Add(notebookList);
        addNotebookButton = new Button { Text = "+", TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat, ForeColor = Theme.Accent, BackColor = Color.White, Size = new Size(38, 38), Margin = new Padding(82, 4, 82, 4), AccessibleName = "Add notebook" };
        addNotebookButton.FlatAppearance.BorderColor = Theme.Border;
        addNotebookButton.Click += (_, _) => AddNotebook();
        Controls.Add(sidebar);
        RefreshNotebooks();

        content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(34, 26, 34, 28), AutoScroll = true };
        Controls.Add(content);
    }

    private Button NavButton(string text, int top, EventHandler action)
    {
        var button = new Button { Text = "   " + text, TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat, ForeColor = Theme.Ink, BackColor = Color.Transparent, Location = new Point(12, top), Size = new Size(210, 34) };
        button.FlatAppearance.BorderSize = 0;
        button.Click += action;
        return button;
    }

    private void RefreshNotebooks()
    {
        notebookList.Controls.Clear();
        foreach (var notebook in service.Notebooks)
        {
            // LOCK: add a 🔒 after the name of any notebook that has a password.
            var item = new Button { Text = $"  {notebook.Initials}    {notebook.Name}" + (locks.HasLock(notebook) ? "  🔒" : ""), TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat, BackColor = notebook == selectedNotebook ? Color.FromArgb(235, 238, 255) : Color.Transparent, ForeColor = Theme.Ink, Size = new Size(202, 38), Margin = new Padding(0, 0, 0, 4) };
            item.FlatAppearance.BorderSize = 0;
            item.Click += (_, _) => { selectedNotebook = notebook; selectedPage = service.GetFirstActivePage(notebook); RefreshNotebooks(); ShowPage(); };
            notebookList.Controls.Add(item);
        }
        notebookList.Controls.Add(addNotebookButton);
    }

    // LOCK: redraw the sidebar (🔒 icons) AND the page. Used after any lock change or unlock.
    private void RefreshAll()
    {
        RefreshNotebooks(); // updates the 🔒 icons in the sidebar
        ShowPage();
    }

    private void ShowPage()
    {
        content.Controls.Clear();
        notebookTitle = new Label { Text = selectedNotebook.Name, Font = new Font("Segoe UI", 10), ForeColor = Theme.Muted, Location = new Point(34, 26), AutoSize = true };
        content.Controls.Add(notebookTitle);

        // LOCK: locked notebook -> show only the unlock screen and stop. No page info is drawn, so nothing leaks.
        if (locks.IsLocked(selectedNotebook))
        {
            lockUi.AddLockedScreen(content, "notebook", selectedNotebook.Name, selectedNotebook, 110, RefreshAll);
            return;
        }

        var addPage = new Button { Text = "+ New page", FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White, Size = new Size(110, 34), Location = new Point(Width - 190, 18), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        addPage.FlatAppearance.BorderSize = 0;
        addPage.Click += (_, _) => { selectedPage = service.AddPage(selectedNotebook); ShowPage(); };
        content.Controls.Add(addPage);

        // LOCK: menu button (Set password / Lock now / Remove lock) for the whole notebook.
        var notebookLock = lockUi.CreateMenuButton("Notebook lock", "this notebook", selectedNotebook, RefreshAll);
        notebookLock.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        notebookLock.Location = new Point(Width - 345, 19);
        content.Controls.Add(notebookLock);

        var pageTabs = new FlowLayoutPanel { Location = new Point(30, 58), Size = new Size(Width - 300, 88), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, AutoScroll = true, WrapContents = false };
        foreach (var page in selectedNotebook.Pages.Where(p => !p.Deleted))
        {
            // LOCK: a locked page shows "🔒 Locked" instead of its text preview.
            var card = new Button { Text = page.Title + Environment.NewLine + "  " + (locks.IsLocked(page) ? "🔒 Locked" : page.Preview), TextAlign = ContentAlignment.TopLeft, FlatStyle = FlatStyle.Flat, BackColor = page == selectedPage ? Color.FromArgb(245, 246, 255) : Color.White, ForeColor = Theme.Ink, Size = new Size(210, 74), Margin = new Padding(0, 0, 10, 0), Padding = new Padding(12, 10, 8, 4) };
            card.FlatAppearance.BorderColor = page == selectedPage ? Theme.Accent : Theme.Border;
            card.Click += (_, _) => { selectedPage = page; ShowPage(); };
            pageTabs.Controls.Add(card);
        }
        var addPageTab = new Button { Text = "+", FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Theme.Accent, Size = new Size(74, 74), Margin = new Padding(0, 0, 10, 0) };
        addPageTab.FlatAppearance.BorderColor = Theme.Border;
        addPageTab.Click += (_, _) => { selectedPage = service.AddPage(selectedNotebook); ShowPage(); };
        pageTabs.Controls.Add(addPageTab);
        content.Controls.Add(pageTabs);

        var divider = new Label { BorderStyle = BorderStyle.Fixed3D, Height = 2, Width = Width - 310, Location = new Point(34, 162), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        content.Controls.Add(divider);

        // LOCK: locked page -> show only the unlock screen (below the tabs) and stop. Title, editor and drawing stay hidden.
        if (locks.IsLocked(selectedPage))
        {
            lockUi.AddLockedScreen(content, "page", selectedPage.Title, selectedPage, 190, RefreshAll);
            return;
        }

        pageTitle = new Label { Text = selectedPage.Title, Font = new Font("Segoe UI", 32, FontStyle.Bold), ForeColor = Theme.Ink, Location = new Point(34, 174), AutoSize = true };
        content.Controls.Add(pageTitle);
        var tools = new FlowLayoutPanel { Location = new Point(34, 226), Size = new Size(Width - 310, 38), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        AddTool(tools, "B", FontStyle.Bold, () => pageEditor.SelectedText = $"**{pageEditor.SelectedText}**");
        AddTool(tools, "I", FontStyle.Italic, () => pageEditor.SelectedText = $"_{pageEditor.SelectedText}_");
        AddTool(tools, "Highlight", FontStyle.Regular, () => pageEditor.SelectedText = $"=={pageEditor.SelectedText}==");
        AddTool(tools, "Underline", FontStyle.Underline, () => pageEditor.SelectedText = $"__{pageEditor.SelectedText}__");
        AddTool(tools, "Draw", FontStyle.Regular, ShowDrawing);
        AddTool(tools, "Move to trash", FontStyle.Regular, () => { service.MoveToTrash(selectedPage); ShowPage(); });
        // LOCK: menu button (Set password / Lock now / Remove lock) for this page, at the end of the toolbar.
        tools.Controls.Add(lockUi.CreateMenuButton("Page lock", "this page", selectedPage, RefreshAll));
        content.Controls.Add(tools);
        pageEditor = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12), ForeColor = Theme.Ink, Location = new Point(34, 290), Size = new Size(Width - 340, 300), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        pageEditor.Text = selectedPage.Content;
        pageEditor.TextChanged += (_, _) => selectedPage.Content = pageEditor.Text;
        content.Controls.Add(pageEditor);
        content.Controls.Add(new Label { Text = "Tip: select text, then use Highlight or Underline. Highlighted passages appear in Study List.", ForeColor = Theme.Muted, Font = new Font("Segoe UI", 9), Location = new Point(34, 605), AutoSize = true });
        if (selectedPage.Strokes.Count > 0)
        {
            content.Controls.Add(new Label { Text = "Saved drawing (click to edit)", ForeColor = Theme.Muted, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(34, 634), AutoSize = true });
            var preview = new CanvasPanel { Location = new Point(34, 658), Size = new Size(Width - 340, 170), BackColor = Color.FromArgb(252, 253, 255), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            preview.Paint += (_, e) => DrawingRenderer.DrawStrokesFit(e.Graphics, selectedPage.Strokes, preview.ClientSize);
            preview.Click += (_, _) => ShowDrawing();
            content.Controls.Add(preview);
        }
    }

    private void ShowDrawing()
    {
        using var dialog = new DrawingDialog(selectedPage.Strokes);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        selectedPage.Strokes = dialog.Strokes;
        ShowPage(); // refresh so the saved drawing shows on the page
    }

    private void AddTool(FlowLayoutPanel panel, string text, FontStyle style, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Theme.Ink, Font = new Font("Segoe UI", 9, style), Padding = new Padding(8, 0, 8, 0) };
        button.FlatAppearance.BorderColor = Theme.Border;
        button.Click += (_, _) => action();
        panel.Controls.Add(button);
    }

    private void ShowStudyList()
    {
        content.Controls.Clear();
        content.Controls.Add(new Label { Text = "Study List", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Theme.Ink, Location = new Point(34, 28), AutoSize = true });
        content.Controls.Add(new Label { Text = "Review the important passages from " + selectedNotebook.Name, ForeColor = Theme.Muted, Location = new Point(36, 76), AutoSize = true });
        // LOCK: a locked notebook must not reveal its highlights.
        if (locks.IsLocked(selectedNotebook))
        {
            content.Controls.Add(new Label { Text = "This notebook is locked. Unlock it to see its study list.", ForeColor = Theme.Muted, Location = new Point(36, 125), AutoSize = true });
            return;
        }
        var y = 125;
        // LOCK: also skip individual locked pages in an unlocked notebook.
        foreach (var page in service.GetHighlightedPages(selectedNotebook).Where(p => !locks.IsLocked(p)))
        {
            var text = page.StudyText;
            var card = new Label { Text = text + Environment.NewLine + "From: " + page.Title, AutoSize = false, Size = new Size(Width - 370, 75), Location = new Point(34, y), Padding = new Padding(16), BackColor = Color.FromArgb(255, 251, 229), ForeColor = Theme.Ink, Font = new Font("Segoe UI", 11) };
            content.Controls.Add(card);
            y += 88;
        }
        if (y == 125) content.Controls.Add(new Label { Text = "No highlighted passages yet. Open a page and highlight key text.", ForeColor = Theme.Muted, Location = new Point(36, 125), AutoSize = true });
    }

    private void ShowTrash()
    {
        content.Controls.Clear();
        content.Controls.Add(new Label { Text = "Trash", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Theme.Ink, Location = new Point(34, 28), AutoSize = true });
        var y = 100;
        foreach (var page in service.GetTrash())
        {
            // LOCK: find the page's notebook, then show "🔒 Locked page" instead of the title if either one is locked.
            var owner = service.Notebooks.First(n => n.Pages.Contains(page));
            var title = locks.IsHidden(owner, page) ? "🔒 Locked page" : page.Title;
            var restore = new Button { Text = "Restore", Location = new Point(500, y), Size = new Size(90, 30) };
            restore.Click += (_, _) => { service.Restore(page); ShowTrash(); };
            content.Controls.Add(new Label { Text = title, Location = new Point(38, y + 7), AutoSize = true });
            content.Controls.Add(restore);
            y += 48;
        }
        if (y == 100) content.Controls.Add(new Label { Text = "Your trash is empty.", ForeColor = Theme.Muted, Location = new Point(38, y), AutoSize = true });
    }

    private void Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        // LOCK: locks.IsHidden makes search skip locked pages, so their text can't be found.
        var result = service.FindFirstMatch(query, locks.IsHidden);
        if (result is null) return;
        selectedNotebook = result.Notebook;
        selectedPage = result.Page;
        ShowPage();
    }

    private void AddNotebook()
    {
        var name = PromptDialog.Show("Name your subject notebook:", "New Subject");
        if (string.IsNullOrWhiteSpace(name)) return;
        selectedNotebook = service.AddNotebook(name);
        selectedPage = selectedNotebook.Pages[0];
        RefreshNotebooks();
        ShowPage();
    }
}