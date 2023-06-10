using System.Data;
using System.Data.Common;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Store;

namespace Dav.AspNetCore.Server.Extensions;

public abstract class SqlLockManager : ILockManager, IDisposable
{
    private readonly SqlLockOptions options;
    private readonly Lazy<DbConnection> connection;

    private static readonly ValueTask<IReadOnlyCollection<LockType>> SupportedLocks = new(new List<LockType>
    {
        LockType.Exclusive,
        LockType.Shared
    });
    
    /// <summary>
    /// Initializes a new <see cref="SqlLockManager"/> class.
    /// </summary>
    /// <param name="options">The sql lock store options.</param>
    protected SqlLockManager(SqlLockOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        
        this.options = options;
        connection = new Lazy<DbConnection>(() => CreateConnection(options.ConnectionString));
    }
    
        /// <summary>
    /// Locks the resource async.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="lockType">The lock type.</param>
    /// <param name="owner">The lock owner.</param>
    /// <param name="recursive">A value indicating whether the lock will be recursive.</param>
    /// <param name="timeout">The lock timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The lock result.</returns>
    public async ValueTask<LockResult> LockAsync(
        Uri uri, 
        LockType lockType, 
        XElement owner, 
        bool recursive, 
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri, nameof(uri));
        ArgumentNullException.ThrowIfNull(owner, nameof(owner));
        
        if (connection.Value.State != ConnectionState.Open)
            await connection.Value.OpenAsync(cancellationToken);
        
        var activeLocks = await GetLocksAsync(uri, cancellationToken);
        if ((activeLocks.All(x => x.LockType == LockType.Shared) &&
             lockType == LockType.Shared) ||
            activeLocks.Count == 0)
        {
            var newLock = new ResourceLock(
                new Uri($"urn:uuid:{Guid.NewGuid():D}"),
                uri,
                lockType,
                owner,
                recursive,
                timeout,
                DateTime.UtcNow);
            
            var depth = uri.LocalPath.Count(x => x == '/') - 1;

            await using var command = GetInsertCommand(
                connection.Value,
                newLock.Id.AbsoluteUri,
                newLock.Uri.LocalPath,
                newLock.LockType,
                newLock.Owner,
                newLock.Recursive,
                (long)newLock.Timeout.TotalSeconds,
                (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds,
                depth);
            
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affectedRows > 0)
                return new LockResult(DavStatusCode.Ok, newLock);
        }
        
        return new LockResult(DavStatusCode.Locked);
    }
        
    /// <summary>
    /// Refreshes the resource lock async.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="token">The lock token.</param>
    /// <param name="timeout">The lock timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The lock result.</returns>
    public async ValueTask<LockResult> RefreshLockAsync(
        Uri uri,
        Uri token, 
        TimeSpan timeout, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri, nameof(uri));
        ArgumentNullException.ThrowIfNull(token, nameof(token));
        
        if (connection.Value.State != ConnectionState.Open)
            await connection.Value.OpenAsync(cancellationToken);

        await using var command = GetActiveLockByIdCommand(
            connection.Value,
            token.AbsoluteUri,
            uri.LocalPath,
            (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetString("Id");
            await using var updateCommand = GetRefreshCommand(
                connection.Value,
                id,
                (long)timeout.TotalSeconds,
                (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);

            var affectedRows = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            if (affectedRows > 0)
                return new LockResult(DavStatusCode.Ok);
        }
        
        return new LockResult(DavStatusCode.PreconditionFailed);
    }

    /// <summary>
    /// Unlocks the resource async.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="token">The lock token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    public async ValueTask<DavStatusCode> UnlockAsync(
        Uri uri, 
        Uri token, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri, nameof(uri));
        ArgumentNullException.ThrowIfNull(token, nameof(token));
        
        if (connection.Value.State != ConnectionState.Open)
            await connection.Value.OpenAsync(cancellationToken);
        
        await using var command = GetActiveLockByIdCommand(
            connection.Value,
            token.AbsoluteUri,
            uri.LocalPath,
            (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetString("Id");
            await using var deleteCommand = GetDeleteCommand(connection.Value, id);

            var affectedRows = await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            if (affectedRows > 0)
                return DavStatusCode.NoContent;
        }

        return DavStatusCode.Conflict;
    }
        
    /// <summary>
    /// Gets all active resource locks async.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of all active resource locks for the given resource.</returns>
    public async ValueTask<IReadOnlyCollection<ResourceLock>> GetLocksAsync(
        Uri uri, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri, nameof(uri));
        
        if (connection.Value.State != ConnectionState.Open)
            await connection.Value.OpenAsync(cancellationToken);

        var allActiveLocks = new List<ResourceLock>();
        var depth = uri.LocalPath.Count(x => x == '/') - 1;

        await using var command = GetActiveLocksCommand(
            connection.Value,
            uri.LocalPath,
            depth,
            (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var resourceLock = new ResourceLock(
                new Uri(reader.GetString("Id")),
                new Uri(reader.GetString("Id")),
                (LockType)reader.GetInt32("LockType"),
                XElement.Parse(reader.GetString("Owner")),
                reader.GetBoolean("Recursive"),
                TimeSpan.FromSeconds(reader.GetInt64("Timeout")),
                DateTime.UnixEpoch + TimeSpan.FromSeconds(reader.GetInt64("Issued")));
            
            allActiveLocks.Add(resourceLock);
        }

        return allActiveLocks;
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
    {
        return SupportedLocks;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        connection.Value.Dispose();
    }

    /// <summary>
    /// Removes stale locks async.
    /// </summary>
    public async Task RemoveStaleLocksAsync(CancellationToken cancellationToken = default)
    {
        if (connection.Value.State != ConnectionState.Open)
            await connection.Value.OpenAsync(cancellationToken);
        
        await using var command = GetDeleteStaleCommand(
            connection.Value,
            (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a db connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The db connection.</returns>
    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Gets the insert command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="id">The id.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="lockType">The lock type.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="recursive">A value indicating whether the lock is recursive.</param>
    /// <param name="timeout">The timeout.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <param name="depth">The depth.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetInsertCommand(
        DbConnection connection,
        string id,
        string uri,
        LockType lockType,
        XElement owner,
        bool recursive,
        long timeout,
        long totalSeconds,
        int depth);

    /// <summary>
    /// Gets the active locks command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="depth">The depth.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetActiveLocksCommand(
        DbConnection connection,
        string uri,
        int depth,
        long totalSeconds);

    /// <summary>
    /// Gets the active lock by id command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="id">The id.</param>
    /// <param name="uri">The uri.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetActiveLockByIdCommand(
        DbConnection connection,
        string id,
        string uri,
        long totalSeconds);

    /// <summary>
    /// Gets the refresh command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="id">The id.</param>
    /// <param name="timeout">The timeout.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetRefreshCommand(
        DbConnection connection,
        string id,
        long timeout,
        long totalSeconds);
    
    /// <summary>
    /// Gets the delete command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="id">The id.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetDeleteCommand(
        DbConnection connection,
        string id);

    /// <summary>
    /// Gets the delete stale locks command.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="totalSeconds">The total seconds.</param>
    /// <returns>The prepared command.</returns>
    protected abstract DbCommand GetDeleteStaleCommand(
        DbConnection connection,
        long totalSeconds);
    
    /// <summary>
    /// Gets the table id.
    /// </summary>
    /// <returns></returns>
    protected string GetTableId()
    {
        return string.IsNullOrWhiteSpace(options.Schema) 
            ? $"{options.Table}" 
            : $"{options.Schema}.{options.Table}";
    }
}