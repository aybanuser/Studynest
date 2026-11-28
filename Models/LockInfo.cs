namespace StudyNest.Models;

/// <summary>
/// What we remember about a password. Pure data: no rules, no UI.
/// The password itself is NEVER stored, only a salt and a hash made from it.
/// Only LockService should read or write these values.
/// </summary>
public sealed class LockInfo
{
    /// <summary>Random bytes mixed into the password, so equal passwords get different hashes.</summary>
    public byte[]? Salt { get; set; }

    /// <summary>The scrambled (one-way) form of salt + password. Compared when unlocking.</summary>
    public byte[]? Hash { get; set; }

    /// <summary>True when a password has been set.</summary>
    public bool IsSet => Hash is not null;

    /// <summary>Forgets the password (used by "Remove lock").</summary>
    public void Clear()
    {
        Salt = null;
        Hash = null;
    }
}