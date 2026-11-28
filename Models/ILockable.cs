namespace StudyNest.Models;

/// <summary>
/// Marks something that can be password-protected (Notebook and PageNote).
/// LockService works with this interface, so one set of rules covers both.
/// </summary>
public interface ILockable
{
    /// <summary>The saved password data. Use Lock.IsSet to know if a password exists.</summary>
    LockInfo Lock { get; }
}