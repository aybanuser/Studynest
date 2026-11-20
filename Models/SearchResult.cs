namespace StudyNest.Models;

/// <summary>A search hit: the page that matched and the notebook that owns it.</summary>
public sealed record SearchResult(Notebook Notebook, PageNote Page);
