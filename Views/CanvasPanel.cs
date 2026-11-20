namespace StudyNest.Views;

/// <summary>A Panel that repaints smoothly (no flicker) and when resized. Used for drawing.</summary>
internal sealed class CanvasPanel : Panel
{
    public CanvasPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }
}
