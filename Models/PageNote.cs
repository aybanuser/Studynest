namespace StudyNest.Models;

/// <summary>One note page: a title, the typed text, and any saved drawing.</summary>
public sealed class PageNote(string title, string content)
{
    public string Title { get; } = title;
    public string Content { get; set; } = content;
    public bool Deleted { get; set; }

    /// <summary>Each inner list is one stroke (one mouse press to release).</summary>
    public List<List<Point>> Strokes { get; set; } = [];

    public string Preview => Content.Length > 38 ? Content[..38] + "..." : Content;

    // Temporary: highlights are still "==text==" symbols inside the text.
    // These two go away when we replace them with real Mark objects.
    public bool HasHighlights => Content.Contains("==");
    public string StudyText => Content.Replace("==", "");
}
