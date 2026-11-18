using System.Drawing.Drawing2D;

namespace StudyNest;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new StudyNestForm());
    }
}

internal sealed class StudyNestForm : Form
{
    private readonly Color ink = Color.FromArgb(32, 38, 48);
    private readonly Color muted = Color.FromArgb(112, 121, 133);
    private readonly Color border = Color.FromArgb(226, 230, 235);
    private readonly Color accent = Color.FromArgb(75, 92, 207);
    private readonly List<Notebook> notebooks =
    [
        new("Computer Science", "CS", Color.FromArgb(229, 235, 255), new("Binary Search Trees", "Review traversal, balancing, and common operations.")),
        new("Data Structures", "DS", Color.FromArgb(231, 247, 239), new("Stacks & Queues", "LIFO and FIFO structures with practical examples.")),
        new("Software Design", "SD", Color.FromArgb(255, 242, 218), new("OOP Principles", "Encapsulation, abstraction, inheritance, and polymorphism."))
    ];

    private Notebook selectedNotebook;
    private PageNote selectedPage;
    private Panel content = null!;
    private Panel notebookList = null!;
    private Label notebookTitle = null!;
    private Label pageTitle = null!;
    private TextBox pageEditor = null!;
    private Panel drawingSurface = null!;

    public StudyNestForm()
    {
        selectedNotebook = notebooks[0];
        selectedPage = selectedNotebook.Pages[0];
        Text = "StudyNest";
        MinimumSize = new Size(1080, 700);
        Size = new Size(1280, 800);
        BackColor = Color.White;
        StartPosition = FormStartPosition.CenterScreen;
        BuildShell();
        ShowPage();
    }

    private void BuildShell()
    {
        var top = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.White };
        top.Controls.Add(new Label { Text = "StudyNest", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = ink, Location = new Point(28, 17), AutoSize = true });
        top.Controls.Add(new Label { Text = "Your notes, in one place.", Font = new Font("Segoe UI", 9), ForeColor = muted, Location = new Point(145, 25), AutoSize = true });
        var search = new TextBox { PlaceholderText = "Search notebooks and pages...", BorderStyle = BorderStyle.FixedSingle, Width = 270, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(Width - 390, 18) };
        search.TextChanged += (_, _) => Search(search.Text);
        top.Controls.Add(search);
        var avatar = new Label { Text = "A", TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(238, 241, 255), ForeColor = accent, Font = new Font("Segoe UI", 10, FontStyle.Bold), Size = new Size(34, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(Width - 78, 17) };
        top.Controls.Add(avatar);
        Controls.Add(top);

        var sidebar = new Panel { Dock = DockStyle.Left, Width = 244, BackColor = Color.FromArgb(249, 250, 252), Padding = new Padding(18, 18, 12, 18) };
        sidebar.Controls.Add(new Label { Text = "WORKSPACE", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = muted, Location = new Point(20, 20), AutoSize = true });
        sidebar.Controls.Add(NavButton("All notes", 52, (_, _) => ShowPage()));
        sidebar.Controls.Add(NavButton("Study List", 91, (_, _) => ShowStudyList()));
        sidebar.Controls.Add(NavButton("Trash", 130, (_, _) => ShowTrash()));
        sidebar.Controls.Add(new Label { Text = "NOTEBOOKS", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = muted, Location = new Point(20, 188), AutoSize = true });
        notebookList = new Panel { Location = new Point(12, 214), Size = new Size(214, 235), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, AutoScroll = true };
        sidebar.Controls.Add(notebookList);
        var add = new Button { Text = "+  New notebook", FlatStyle = FlatStyle.Flat, ForeColor = accent, BackColor = Color.White, Location = new Point(16, 462), Size = new Size(202, 38), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        add.FlatAppearance.BorderColor = border;
        add.Click += (_, _) => AddNotebook();
        sidebar.Controls.Add(add);
        Controls.Add(sidebar);
        RefreshNotebooks();

        content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(34, 26, 34, 28), AutoScroll = true };
        Controls.Add(content);
    }

    private Button NavButton(string text, int top, EventHandler action)
    {
        var button = new Button { Text = "   " + text, TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat, ForeColor = ink, BackColor = Color.Transparent, Location = new Point(12, top), Size = new Size(210, 34) };
        button.FlatAppearance.BorderSize = 0;
        button.Click += action;
        return button;
    }

    private void RefreshNotebooks()
    {
        notebookList.Controls.Clear();
        var y = 0;
        foreach (var notebook in notebooks)
        {
            var item = new Button { Text = $"  {notebook.Initials}    {notebook.Name}", TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat, BackColor = notebook == selectedNotebook ? Color.FromArgb(235, 238, 255) : Color.Transparent, ForeColor = ink, Location = new Point(0, y), Size = new Size(202, 38) };
            item.FlatAppearance.BorderSize = 0;
            item.Click += (_, _) => { selectedNotebook = notebook; selectedPage = notebook.Pages.FirstOrDefault() ?? new PageNote("Untitled page", "Start writing your notes here."); if (!notebook.Pages.Contains(selectedPage)) notebook.Pages.Add(selectedPage); RefreshNotebooks(); ShowPage(); };
            notebookList.Controls.Add(item);
            y += 42;
        }
    }

    private void ShowPage()
    {
        content.Controls.Clear();
        notebookTitle = new Label { Text = selectedNotebook.Name, Font = new Font("Segoe UI", 10), ForeColor = muted, Location = new Point(34, 26), AutoSize = true };
        content.Controls.Add(notebookTitle);
        var addPage = new Button { Text = "+ New page", FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.White, Size = new Size(110, 34), Location = new Point(Width - 190, 18), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        addPage.FlatAppearance.BorderSize = 0;
        addPage.Click += (_, _) => { selectedPage = new PageNote("Untitled page", "Start writing your notes here."); selectedNotebook.Pages.Add(selectedPage); ShowPage(); };
        content.Controls.Add(addPage);
        var pages = new FlowLayoutPanel { Location = new Point(30, 58), Size = new Size(Width - 300, 88), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, AutoScroll = true, WrapContents = false };
        foreach (var page in selectedNotebook.Pages.Where(p => !p.Deleted))
        {
            var card = new Button { Text = page.Title + Environment.NewLine + "  " + page.Preview, TextAlign = ContentAlignment.TopLeft, FlatStyle = FlatStyle.Flat, BackColor = page == selectedPage ? Color.FromArgb(245, 246, 255) : Color.White, ForeColor = ink, Size = new Size(210, 74), Margin = new Padding(0, 0, 10, 0), Padding = new Padding(12, 10, 8, 4) };
            card.FlatAppearance.BorderColor = page == selectedPage ? accent : border;
            card.Click += (_, _) => { selectedPage = page; ShowPage(); };
            pages.Controls.Add(card);
        }
        content.Controls.Add(pages);
        var divider = new Label { BorderStyle = BorderStyle.Fixed3D, Height = 2, Width = Width - 310, Location = new Point(34, 162), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        content.Controls.Add(divider);
        pageTitle = new Label { Text = selectedPage.Title, Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = ink, Location = new Point(34, 184), AutoSize = true };
        content.Controls.Add(pageTitle);
        var tools = new FlowLayoutPanel { Location = new Point(34, 236), Size = new Size(Width - 310, 38), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        AddTool(tools, "B", FontStyle.Bold, () => pageEditor.SelectedText = $"**{pageEditor.SelectedText}**");
        AddTool(tools, "I", FontStyle.Italic, () => pageEditor.SelectedText = $"_{pageEditor.SelectedText}_");
        AddTool(tools, "Highlight", FontStyle.Regular, () => pageEditor.SelectedText = $"=={pageEditor.SelectedText}==");
        AddTool(tools, "Underline", FontStyle.Underline, () => pageEditor.SelectedText = $"__{pageEditor.SelectedText}__");
        AddTool(tools, "Draw", FontStyle.Regular, ShowDrawing);
        AddTool(tools, "Move to trash", FontStyle.Regular, () => { selectedPage.Deleted = true; ShowPage(); });
        content.Controls.Add(tools);
        pageEditor = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12), ForeColor = ink, Location = new Point(34, 290), Size = new Size(Width - 340, 300), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        pageEditor.Text = selectedPage.Content;
        pageEditor.TextChanged += (_, _) => selectedPage.Content = pageEditor.Text;
        content.Controls.Add(pageEditor);
        content.Controls.Add(new Label { Text = "Tip: select text, then use Highlight or Underline. Highlighted passages appear in Study List.", ForeColor = muted, Font = new Font("Segoe UI", 9), Location = new Point(34, 605), AutoSize = true });
    }

    private void AddTool(FlowLayoutPanel panel, string text, FontStyle style, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = ink, Font = new Font("Segoe UI", 9, style), Padding = new Padding(8, 0, 8, 0) };
        button.FlatAppearance.BorderColor = border;
        button.Click += (_, _) => action();
        panel.Controls.Add(button);
    }

    private void ShowDrawing()
    {
        var dialog = new Form { Text = "Drawing block", Size = new Size(700, 500), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White };
        drawingSurface = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(252, 253, 255), Cursor = Cursors.Cross };
        var points = new List<Point>();
        drawingSurface.MouseDown += (_, e) => points.Add(e.Location);
        drawingSurface.MouseMove += (_, e) => { if (e.Button == MouseButtons.Left) { points.Add(e.Location); drawingSurface.Invalidate(); } };
        drawingSurface.Paint += (_, e) =>
        {
            if (points.Count <= 1) return;
            using var pen = new Pen(accent, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            for (var i = 1; i < points.Count; i++)
                e.Graphics.DrawLine(pen, points[i - 1], points[i]);
        };
        var undo = new Button { Text = "Clear drawing", Dock = DockStyle.Bottom, Height = 36 };
        undo.Click += (_, _) => { points.Clear(); drawingSurface.Invalidate(); };
        dialog.Controls.Add(drawingSurface);
        dialog.Controls.Add(undo);
        dialog.ShowDialog(this);
    }

    private void ShowStudyList()
    {
        content.Controls.Clear();
        content.Controls.Add(new Label { Text = "Study List", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = ink, Location = new Point(34, 28), AutoSize = true });
        content.Controls.Add(new Label { Text = "Review the important passages from " + selectedNotebook.Name, ForeColor = muted, Location = new Point(36, 76), AutoSize = true });
        var y = 125;
        foreach (var page in selectedNotebook.Pages.Where(p => !p.Deleted && p.Content.Contains("==")))
        {
            var text = page.Content.Replace("==", "");
            var card = new Label { Text = text + Environment.NewLine + "From: " + page.Title, AutoSize = false, Size = new Size(Width - 370, 75), Location = new Point(34, y), Padding = new Padding(16), BackColor = Color.FromArgb(255, 251, 229), ForeColor = ink, Font = new Font("Segoe UI", 11) };
            content.Controls.Add(card);
            y += 88;
        }
        if (y == 125) content.Controls.Add(new Label { Text = "No highlighted passages yet. Open a page and highlight key text.", ForeColor = muted, Location = new Point(36, 125), AutoSize = true });
    }

    private void ShowTrash()
    {
        content.Controls.Clear();
        content.Controls.Add(new Label { Text = "Trash", Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = ink, Location = new Point(34, 28), AutoSize = true });
        var y = 100;
        foreach (var page in notebooks.SelectMany(n => n.Pages).Where(p => p.Deleted))
        {
            var restore = new Button { Text = "Restore", Location = new Point(500, y), Size = new Size(90, 30) };
            restore.Click += (_, _) => { page.Deleted = false; ShowTrash(); };
            content.Controls.Add(new Label { Text = page.Title, Location = new Point(38, y + 7), AutoSize = true });
            content.Controls.Add(restore);
            y += 48;
        }
        if (y == 100) content.Controls.Add(new Label { Text = "Your trash is empty.", ForeColor = muted, Location = new Point(38, y), AutoSize = true });
    }

    private void Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        var matches = notebooks.SelectMany(n => n.Pages.Where(p => !p.Deleted && (p.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || p.Content.Contains(query, StringComparison.OrdinalIgnoreCase)))).ToList();
        if (matches.Count == 0) return;
        selectedNotebook = notebooks.First(n => n.Pages.Contains(matches[0]));
        selectedPage = matches[0];
        ShowPage();
    }

    private void AddNotebook()
    {
        var name = Prompt("Name your subject notebook:", "New Subject");
        if (string.IsNullOrWhiteSpace(name)) return;
        notebooks.Add(new Notebook(name, name[..Math.Min(2, name.Length)].ToUpperInvariant(), Color.FromArgb(238, 241, 255), new("First page", "Start writing your notes here.")));
        selectedNotebook = notebooks[^1];
        selectedPage = selectedNotebook.Pages[0];
        RefreshNotebooks();
        ShowPage();
    }

    private static string Prompt(string message, string defaultValue)
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

    private sealed record Notebook(string Name, string Initials, Color Color, PageNote FirstPage)
    {
        public List<PageNote> Pages { get; } = [FirstPage];
    }

    private sealed class PageNote(string title, string content)
    {
        public string Title { get; } = title;
        public string Content { get; set; } = content;
        public bool Deleted { get; set; }
        public string Preview => Content.Length > 38 ? Content[..38] + "..." : Content;
    }
}
