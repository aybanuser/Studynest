using System.Security.Cryptography;
using StudyNest.Models;

namespace StudyNest.Services;

/// <summary>
/// All the locking rules. It never touches a window or a control (no UI code here).
/// Key idea: "locked" means the item HAS a password AND was NOT unlocked in this session.
/// </summary>
public sealed class LockService
{
    /// <summary>Shortest password allowed. PasswordDialog reads this too.</summary>
    public const int MinPasswordLength = 4;

    // Hashing settings. More iterations = slower for someone guessing passwords.
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    // Items unlocked since the app started. Lives only in memory, so closing the app locks everything.
    // HashSet compares by reference here, so each page/notebook object is tracked on its own.
    private readonly HashSet<ILockable> unlocked = [];

    /// <summary>Does this item have a password at all? (Used for the 🔒 icons.)</summary>
    public bool HasLock(ILockable item) => item.Lock.IsSet;

    /// <summary>Is this item closed right now? True only if it has a password and was not unlocked yet.</summary>
    public bool IsLocked(ILockable item) => item.Lock.IsSet && !unlocked.Contains(item);

    /// <summary>A page is hidden if the page itself OR the notebook that holds it is locked.</summary>
    public bool IsHidden(Notebook notebook, PageNote page) => IsLocked(notebook) || IsLocked(page);

    /// <summary>Sets a password. The item stays open this session until "Lock now".</summary>
    public void SetPassword(ILockable item, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize); // new random salt every time
        item.Lock.Salt = salt;
        item.Lock.Hash = Hash(password, salt);
        unlocked.Add(item); // the user just typed the password, so don't lock them out right away
    }

    /// <summary>Checks the password. If correct, the item stays unlocked for this session.</summary>
    public bool TryUnlock(ILockable item, string password)
    {
        if (!item.Lock.IsSet) return true; // nothing to unlock
        if (!Matches(item, password)) return false;
        unlocked.Add(item);
        return true;
    }

    /// <summary>Closes the item again right away. The password stays set.</summary>
    public void LockNow(ILockable item) => unlocked.Remove(item);

    /// <summary>Deletes the password, but only if the given password is correct.</summary>
    public bool RemoveLock(ILockable item, string password)
    {
        if (!Matches(item, password)) return false;
        item.Lock.Clear();
        unlocked.Remove(item);
        return true;
    }

    /// <summary>Hashes the typed password with the saved salt and compares it to the saved hash.</summary>
    private static bool Matches(ILockable item, string password)
    {
        if (item.Lock.Salt is null || item.Lock.Hash is null) return false;
        var attempt = Hash(password, item.Lock.Salt);
        // FixedTimeEquals takes the same time for any input, so timing can't leak information.
        return CryptographicOperations.FixedTimeEquals(attempt, item.Lock.Hash);
    }

    /// <summary>One-way scramble (PBKDF2). The same password + salt always gives the same hash.</summary>
    private static byte[] Hash(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
    }
}