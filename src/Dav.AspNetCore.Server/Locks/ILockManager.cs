using System.Xml.Linq;
using Dav.AspNetCore.Server.Store;

namespace Dav.AspNetCore.Server.Locks;

public interface ILockManager
{
    /// <summary>
    /// Locks the resource async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="lockType">The lock type.</param>
    /// <param name="owner">The lock owner.</param>
    /// <param name="recursive">A value indicating whether the lock will be recursive.</param>
    /// <param name="timeout">The lock timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The lock result.</returns>
    ValueTask<LockResult> LockAsync(
        ResourcePath path,
        LockType lockType,
        XElement owner,
        bool recursive,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the resource lock async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="token">The lock token.</param>
    /// <param name="timeout">The lock timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The lock result.</returns>
    ValueTask<LockResult> RefreshLockAsync(
        ResourcePath path,
        Uri token,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks the resource async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="token">The lock token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    ValueTask<DavStatusCode> UnlockAsync(
        ResourcePath path,
        Uri token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active resource locks async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all active resource locks for the given resource.</returns>
    ValueTask<IReadOnlyCollection<ResourceLock>> GetLocksAsync(
        ResourcePath path, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported locks async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of available lock types for the given resource.</returns>
    ValueTask<IReadOnlyCollection<LockType>> GetSupportedLocksAsync(
        IStoreItem item, 
        CancellationToken cancellationToken = default);
}