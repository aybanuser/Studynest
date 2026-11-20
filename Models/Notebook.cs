namespace StudyNest.Models;

/// <summary>A subject notebook that holds a list of pages.</summary>
public sealed class Notebook
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
}
