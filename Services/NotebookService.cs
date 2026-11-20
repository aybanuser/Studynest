using StudyNest.Models;

namespace StudyNest.Services;

/// <summary>
/// All the app's rules and data in one place. It never touches a window or a control,
/// so it can be tested from a plain console project.
/// </summary>
public sealed class NotebookService
{
    // Sample data (will be replaced by the database later).
    public List<Notebook> Notebooks { get; } =
    [
        new("Computer Science", "CS", new("Binary Search Trees", "Review traversal, balancing, and common operations.")),
        new("Data Structures", "DS", new("Stacks & Queues", "LIFO and FIFO structures with practical examples.")),
        new("Software Design", "SD", new("OOP Principles", "Encapsulation, abstraction, inheritance, and polymorphism."))
    ];

    public Notebook AddNotebook(string name)
    {
        var initials = name[..Math.Min(2, name.Length)].ToUpperInvariant();
        var notebook = new Notebook(name, initials, new PageNote("First page", "Start writing your notes here."));
        Notebooks.Add(notebook);
        return notebook;
    }

    public PageNote AddPage(Notebook notebook)
    {
        var page = new PageNote("Untitled page", "Start writing your notes here.");
        notebook.Pages.Add(page);
        return page;
    }

    /// <summary>First page that is not in Trash. Creates a new page if there is none.</summary>
    public PageNote GetFirstActivePage(Notebook notebook)
    {
        return notebook.Pages.FirstOrDefault(p => !p.Deleted) ?? AddPage(notebook);
    }

    public void MoveToTrash(PageNote page) => page.Deleted = true;

    public void Restore(PageNote page) => page.Deleted = false;

    public IEnumerable<PageNote> GetTrash()
    {
        return Notebooks.SelectMany(n => n.Pages).Where(p => p.Deleted);
    }

    public IEnumerable<PageNote> GetHighlightedPages(Notebook notebook)
    {
        return notebook.Pages.Where(p => !p.Deleted && p.HasHighlights);
    }

    /// <summary>Finds the first page (not in Trash) whose title or text contains the query.</summary>
    public SearchResult? FindFirstMatch(string query)
    {
        foreach (var notebook in Notebooks)
        {
            var page = notebook.Pages.FirstOrDefault(p => !p.Deleted &&
                (p.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                 p.Content.Contains(query, StringComparison.OrdinalIgnoreCase)));
            if (page is not null) return new SearchResult(notebook, page);
        }
        return null;
    }
}
