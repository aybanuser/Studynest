namespace StudyNest.Views;

/// <summary>Popup window for drawing. Call ShowDialog, then read Strokes if the result is OK.</summary>
internal sealed class DrawingDialog : Form
{
    /// <summary>The strokes the user chose to keep (only changes when Save is pressed).</summary>
    public List<List<Point>> Strokes { get; private set; }

    public DrawingDialog(List<List<Point>> existingStrokes)
    {
        Strokes = existingStrokes;

        // Work on a copy so "Save" decides whether the changes are kept.
        var strokes = existingStrokes.Select(stroke => new List<Point>(stroke)).ToList();
        List<Point>? current = null;

        Text = "Drawing block";
        Size = new Size(700, 540);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;

        var surface = new CanvasPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(252, 253, 255), Cursor = Cursors.Cross };

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
        surface.Paint += (_, e) => DrawingRenderer.DrawStrokes(e.Graphics, strokes);

        var bar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = Color.FromArgb(249, 250, 252), Padding = new Padding(8, 6, 8, 6) };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };

        var save = new Button { Text = "Save", Size = new Size(90, 32), FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.White };
        save.FlatAppearance.BorderSize = 0;
        save.Click += (_, _) =>
        {
            Strokes = strokes.Where(stroke => stroke.Count > 0).Select(stroke => new List<Point>(stroke)).ToList();
            DialogResult = DialogResult.OK;
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
            if (saveDialog.ShowDialog(this) != DialogResult.OK) return;
            using var bitmap = new Bitmap(Math.Max(1, surface.ClientSize.Width), Math.Max(1, surface.ClientSize.Height));
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                DrawingRenderer.DrawStrokes(g, strokes);
            }
            bitmap.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
        };

        buttons.Controls.AddRange([save, undo, clear, exportPng]);
        bar.Controls.Add(buttons);
        Controls.Add(surface);
        Controls.Add(bar);
    }
}
