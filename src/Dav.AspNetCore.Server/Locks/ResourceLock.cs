using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Locks;

/// <summary>
/// Initializes a new <see cref="ResourceLock"/> class.
/// </summary>
/// <param name="Id">The resource lock id.</param>
/// <param name="Path">The resource path.</param>
/// <param name="LockType">The lock type.</param>
/// <param name="Owner">The owner.</param>
/// <param name="Recursive">A value indicating whether the resource lock is recursive.</param>
/// <param name="Timeout">The lock timeout.</param>
/// <param name="IssueDate">The lock issue date.</param>
public record ResourceLock(
    Uri Id,
    ResourcePath Path,
    LockType LockType,
    XElement Owner,
    bool Recursive,
    TimeSpan Timeout,
    DateTime IssueDate)
{
    /// <summary>
    /// A value indicating whether the lock is active.
    /// </summary>
    public bool IsActive => Timeout == TimeSpan.Zero || IssueDate + Timeout > DateTime.UtcNow;
    
    /// <summary>
    /// Gets the expire date.
    /// </summary>
    public DateTime ExpireDate => Timeout == TimeSpan.Zero ? DateTime.MaxValue : IssueDate + Timeout;
}