using System.Collections.Concurrent;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Store;

namespace Dav.AspNetCore.Server.Locks;

public sealed class InMemoryLockManager : ILockManager
{
    private readonly ConcurrentDictionary<Uri, ResourceLock> locks = new();
    
    private static readonly ValueTask<IReadOnlyCollection<LockType>> SupportedLocks = new(new List<LockType>
    {
        LockType.Exclusive,
        LockType.Shared
    });

    /// <summary>
    /// Initializes a new <see cref="InMemoryLockManager"/> class.
    /// </summary>
    /// <param name="locks">The pre population locks.</param>
    public InMemoryLockManager(IEnumerable<ResourceLock> locks)
    {
        ArgumentNullException.ThrowIfNull(locks, nameof(locks));
        foreach (var resourceLock in locks)
        {
            this.locks[resourceLock.Id] = resourceLock;
        }
    }

    /// <summary>
    /// Gets all hold locks.
    /// </summary>
    public IReadOnlyCollection<ResourceLock> Locks => locks.Values.ToList().AsReadOnly();

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
    public async ValueTask<LockResult> LockAsync(
        ResourcePath path, 
        LockType lockType, 
        XElement owner, 
        bool recursive, 
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        ArgumentNullException.ThrowIfNull(owner, nameof(owner));
        
        var activeLocks = await GetLocksAsync(path, cancellationToken);
        if ((activeLocks.All(x => x.LockType == LockType.Shared) &&
             lockType == LockType.Shared) ||
            activeLocks.Count == 0)
        {
            var newLock = new ResourceLock(
                new Uri($"urn:uuid:{Guid.NewGuid():D}"),
                path,
                lockType,
                owner,
                recursive,
                timeout,
                DateTime.UtcNow);
                
            locks.TryAdd(newLock.Id, newLock);
            return new LockResult(DavStatusCode.Ok, newLock);
        }
        
        return new LockResult(DavStatusCode.Locked);
    }

    /// <summary>
    /// Refreshes the resource lock async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="token">The lock token.</param>
    /// <param name="timeout">The lock timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The lock result.</returns>
    public ValueTask<LockResult> RefreshLockAsync(
        ResourcePath path,
        Uri token, 
        TimeSpan timeout, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        ArgumentNullException.ThrowIfNull(token, nameof(token));
        
        var activeLock = locks.Values.FirstOrDefault(x => x.Path == path && x.Id == token && x.IsActive);
        if (activeLock == null)
            return new ValueTask<LockResult>(new LockResult(DavStatusCode.PreconditionFailed));

        var refreshLock = activeLock with
        {
            Timeout = timeout,
            IssueDate = DateTime.UtcNow
        };

        if (!locks.TryRemove(activeLock.Id, out _))
            return new ValueTask<LockResult>(new LockResult(DavStatusCode.PreconditionFailed));

        return !locks.TryAdd(refreshLock.Id, refreshLock) 
            ? new ValueTask<LockResult>(new LockResult(DavStatusCode.PreconditionFailed)) 
            : new ValueTask<LockResult>(new LockResult(DavStatusCode.Ok, refreshLock));
    }

    /// <summary>
    /// Unlocks the resource async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="token">The lock token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    public ValueTask<DavStatusCode> UnlockAsync(
        ResourcePath path,
        Uri token, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        ArgumentNullException.ThrowIfNull(token, nameof(token));
        
        var activeLock = locks.Values.FirstOrDefault(x => x.Path == path && x.Id == token && x.IsActive);
        if (activeLock == null)
            return new ValueTask<DavStatusCode>(DavStatusCode.Conflict);

        return locks.TryRemove(activeLock.Id, out _) 
            ? new ValueTask<DavStatusCode>(DavStatusCode.NoContent) 
            : new ValueTask<DavStatusCode>(DavStatusCode.Conflict);
    }

    /// <summary>
    /// Gets all active resource locks async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all active resource locks for the given store item.</returns>
    public ValueTask<IReadOnlyCollection<ResourceLock>> GetLocksAsync(
        ResourcePath path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        
        var allActiveLocks = new List<ResourceLock>();
        var pathParts = Uri.UnescapeDataString(path).Split('/');
        if (path.ToString().Equals("/"))
            pathParts = new[] { "" };
            
        var currentPath = string.Empty;
            
        for (var i = 0; i < pathParts.Length; i++)
        {
            currentPath += currentPath.Equals("/") ? pathParts[i] : $"/{pathParts[i]}";
            var activeLocks = locks.Values
                .Where(x => x.Path == currentPath && x.IsActive && (x.Recursive || i == pathParts.Length - 1))
                .ToList();
                
            allActiveLocks.AddRange(activeLocks);
        }

        return ValueTask.FromResult<IReadOnlyCollection<ResourceLock>>(allActiveLocks);
    }

    /// <summary>
    /// Gets the supported locks async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of available lock types for the given resource.</returns>
    public ValueTask<IReadOnlyCollection<LockType>> GetSupportedLocksAsync(
        IStoreItem item,
        CancellationToken cancellationToken = default)
        => SupportedLocks;
}