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
        new("Computer Science", "CS", Color.FromArgb(255, 235, 255), new("Binary Search Trees", "Review traversal, balancing, and common operations.")),
        new("Data Structures", "DS", Color.FromArgb(231, 247, 239), new("Stacks & Queues", "LIFO and FIFO structures with practical examples.")),
        new("Software Design", "SD", Color.FromArgb(255, 242, 218), new("OOP Principles", "Encapsulation, abstraction, inheritance, and polymorphism."))
    ];

    private Notebook selectedNotebook;
    private PageNote selectedPage;
    private Panel content = null!;
    private Panel sidebar = null!;
    private FlowLayoutPanel notebookList = null!;
    private Button addNotebookButton = null!;
    private Label notebookTitle = null!;
    private Label pageTitle = null!;
    private TextBox pageEditor = null!;
    private Panel drawingSurface = null!;
    private System.Windows.Forms.Timer sidebarTimer = null!;

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
        var toggleSidebar = new Button { Text = "☰", Font = new Font("Segoe UI", 12), FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = ink, Location = new Point(18, 17), Size = new Size(34, 34), AccessibleName = "Toggle sidebar" };
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
        top.Controls.Add(new Label { Text = "StudyNest", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = ink, Location = new Point(68, 11), AutoSize = true });
        top.Controls.Add(new Label { Text = "Your notes, in one place.", Font = new Font("Segoe UI", 9), ForeColor = muted, Location = new Point(211, 25), AutoSize = true });
        var search = new TextBox { PlaceholderText = "Search notebooks and pages...", BorderStyle = BorderStyle.FixedSingle, Width = 270, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(Width - 390, 18) };
        search.TextChanged += (_, _) => Search(search.Text);
        top.Controls.Add(search);
        var avatar = new Label { Text = "A", TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(238, 241, 255), ForeColor = accent, Font = new Font("Segoe UI", 10, FontStyle.Bold), Size = new Size(34, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(Width - 78, 17) };
        top.Controls.Add(avatar);
        Controls.Add(top);

        sidebar = new Panel { Dock = DockStyle.Left, Width = 0, BackColor = Color.FromArgb(249, 250, 252), Padding = new Padding(18, 18, 12, 18) };
        sidebar.Controls.Add(new Label { Text = "WORKSPACE", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = muted, Location = new Point(20, 20), AutoSize = true });
        sidebar.Controls.Add(NavButton("All notes", 52, (_, _) => ShowPage()));
        sidebar.Controls.Add(NavButton("Study List", 91, (_, _) => ShowStudyList()));
        sidebar.Controls.Add(NavButton("Trash", 130, (_, _) => ShowTrash()));
        sidebar.Controls.Add(new Label { Text = "NOTEBOOKS", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = muted, Location = new Point(20, 188), AutoSize = true });
        notebookList = new FlowLayoutPanel { Location = new Point(12, 214), Size = new Size(214, 235), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        sidebar.Controls.Add(notebookList);
        addNotebookButton = new Button { Text = "+", TextAlign = ContentAlignment.MiddleCenter, FlatStyle = FlatStyle.Flat, ForeColor = accent, BackColor = Color.White, Size = new Size(38, 38), Margin = new Padding(82, 4, 82, 4), AccessibleName = "Add notebook" };
        addNotebookButton.FlatAppearance.BorderColor = border;
        addNotebookButton.Click += (_, _) => AddNotebook();
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
        foreach (var notebook in notebooks)
        {
            var item = new Button { Text = $"  {notebook.Initials}    {notebook.Name}", TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat, BackColor = notebook == selectedNotebook ? Color.FromArgb(235, 238, 255) : Color.Transparent, ForeColor = ink, Size = new Size(202, 38), Margin = new Padding(0, 0, 0, 4) };
            item.FlatAppearance.BorderSize = 0;
            item.Click += (_, _) => { selectedNotebook = notebook; selectedPage = notebook.Pages.FirstOrDefault() ?? new PageNote("Untitled page", "Start writing your notes here."); if (!notebook.Pages.Contains(selectedPage)) notebook.Pages.Add(selectedPage); RefreshNotebooks(); ShowPage(); };
            notebookList.Controls.Add(item);
        }
        notebookList.Controls.Add(addNotebookButton);
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
        pageTitle = new Label { Text = selectedPage.Title, Font = new Font("Segoe UI", 32, FontStyle.Bold), ForeColor = ink, Location = new Point(34, 174), AutoSize = true };
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
        if (selectedPage.Strokes.Count > 0)
        {
            content.Controls.Add(new Label { Text = "Saved drawing (click to edit)", ForeColor = muted, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(34, 634), AutoSize = true });
            var preview = new CanvasPanel { Location = new Point(34, 658), Size = new Size(Width - 340, 170), BackColor = Color.FromArgb(252, 253, 255), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            preview.Paint += (_, e) => DrawStrokesFit(e.Graphics, selectedPage.Strokes, preview.ClientSize);
            preview.Click += (_, _) => ShowDrawing();
            content.Controls.Add(preview);
        }
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
        // Work on a copy so "Save" decides whether the changes are kept.
        var strokes = selectedPage.Strokes.Select(stroke => new List<Point>(stroke)).ToList();
        List<Point>? current = null;

        var dialog = new Form { Text = "Drawing block", Size = new Size(700, 540), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White };
        var surface = new CanvasPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(252, 253, 255), Cursor = Cursors.Cross };
        drawingSurface = surface;

        // Every mouse press starts a NEW stroke, so lifting the mouse cuts the line.
        surface.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            current = [e.Location];
            strokes.Add(current);
            surface.Invalidate();
        };
        surface.MouseMove += (_, e) =>
        {
            if (current is null || e.Button != MouseButtons.Left) return;
            current.Add(e.Location);
            surface.Invalidate();
        };
        surface.MouseUp += (_, _) => current = null;
        surface.Paint += (_, e) => DrawStrokes(e.Graphics, strokes);

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = Color.FromArgb(249, 250, 252), Padding = new Padding(8, 6, 8, 6) };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };

        var save = new Button { Text = "Save", Size = new Size(90, 32), FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.White };
        save.FlatAppearance.BorderSize = 0;
        save.Click += (_, _) =>
        {
            selectedPage.Strokes = strokes.Where(stroke => stroke.Count > 0).Select(stroke => new List<Point>(stroke)).ToList();
            dialog.DialogResult = DialogResult.OK;
        };

        var undo = new Button { Text = "Undo line", Size = new Size(90, 32) };
        undo.Click += (_, _) =>
        {
            if (strokes.Count > 0) strokes.RemoveAt(strokes.Count - 1);
            surface.Invalidate();
        };

        var clear = new Button { Text = "Clear drawing", Size = new Size(100, 32) };
        clear.Click += (_, _) => { strokes.Clear(); surface.Invalidate(); };

        var exportPng = new Button { Text = "Save as PNG", Size = new Size(100, 32) };
        exportPng.Click += (_, _) =>
        {
            using var saveDialog = new SaveFileDialog { Filter = "PNG image|*.png", FileName = "drawing.png" };
            if (saveDialog.ShowDialog(dialog) != DialogResult.OK) return;
            using var bitmap = new Bitmap(Math.Max(1, surface.ClientSize.Width), Math.Max(1, surface.ClientSize.Height));
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                DrawStrokes(g, strokes);
            }
            bitmap.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
        };

        buttons.Controls.AddRange([save, undo, clear, exportPng]);
        bar.Controls.Add(buttons);
        dialog.Controls.Add(surface);
        dialog.Controls.Add(bar);

        var saved = dialog.ShowDialog(this) == DialogResult.OK;
        dialog.Dispose();
        if (saved) ShowPage(); // refresh so the saved drawing shows on the page
    }

    private void DrawStrokes(Graphics g, List<List<Point>> strokes)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(accent, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var dot = new SolidBrush(accent);
        foreach (var stroke in strokes)
        {
            if (stroke.Count == 1)
                g.FillEllipse(dot, stroke[0].X - 1.5f, stroke[0].Y - 1.5f, 3, 3);
            else if (stroke.Count > 1)
                g.DrawLines(pen, stroke.ToArray()); // one polyline per stroke, never joined to the next
        }
    }

    private void DrawStrokesFit(Graphics g, List<List<Point>> strokes, Size target)
    {
        var all = strokes.SelectMany(stroke => stroke).ToList();
        if (all.Count == 0) return;
        int minX = all.Min(pt => pt.X), maxX = all.Max(pt => pt.X);
        int minY = all.Min(pt => pt.Y), maxY = all.Max(pt => pt.Y);
        float w = Math.Max(1, maxX - minX), h = Math.Max(1, maxY - minY);
        const int pad = 12;
        var scale = Math.Min(1f, Math.Min((target.Width - 2 * pad) / w, (target.Height - 2 * pad) / h));
        g.TranslateTransform(pad + (target.Width - 2 * pad - w * scale) / 2, pad + (target.Height - 2 * pad - h * scale) / 2);
        g.ScaleTransform(scale, scale);
        g.TranslateTransform(-minX, -minY);
        DrawStrokes(g, strokes);
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

    private sealed class CanvasPanel : Panel
    {
        public CanvasPanel() { DoubleBuffered = true; ResizeRedraw = true; }
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
        public List<List<Point>> Strokes { get; set; } = [];
        public string Preview => Content.Length > 38 ? Content[..38] + "..." : Content;
    }
}