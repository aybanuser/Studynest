namespace StudyNest.Models;

/// <summary>A subject notebook that holds a list of pages.</summary>
public sealed class Notebook : ILockable
{
    public Notebook(string name, string initials, PageNote firstPage)
    {
        Name = name;
        Initials = initials;
        Pages.Add(firstPage);
    }

    public string Name { get; }
    public string Initials { get; }
    public List<PageNote> Pages { get; } = [];
    /// <summary>Password data for this notebook. Locking a notebook hides all of its pages.</summary>
    public LockInfo Lock { get; } = new();
}