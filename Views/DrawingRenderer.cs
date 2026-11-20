using System.Drawing.Drawing2D;

namespace StudyNest.Views;

internal static class DrawingRenderer
{
    public static void DrawStrokes(Graphics g, List<List<Point>> strokes)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Theme.Accent, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var dot = new SolidBrush(Theme.Accent);
        foreach (var stroke in strokes)
        {
            if (stroke.Count == 1)
                g.FillEllipse(dot, stroke[0].X - 1.5f, stroke[0].Y - 1.5f, 3, 3);
            else if (stroke.Count > 1)
                g.DrawLines(pen, stroke.ToArray()); // one polyline per stroke, never joined to the next
        }
    }

    public static void DrawStrokesFit(Graphics g, List<List<Point>> strokes, Size target)
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
}
