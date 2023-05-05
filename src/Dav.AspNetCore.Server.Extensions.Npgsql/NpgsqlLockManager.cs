using System.Data;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Store;
using Npgsql;

namespace Dav.AspNetCore.Server.Extensions.Npgsql;

public class NpgsqlLockManager : ILockManager, IDisposable
{
    private static readonly ValueTask<IReadOnlyCollection<LockType>> SupportedLocks = new(new List<LockType>
    {
        LockType.Exclusive,
        LockType.Shared
    });

    private readonly NpgsqlConnection connection;

    /// <summary>
    /// Initializes a new <see cref="NpgsqlLockManager"/> class.
    /// </summary>
    /// <param name="options">The npgsql options.</param>
    public NpgsqlLockManager(NpgsqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        connection = new NpgsqlConnection(options.ConnectionString);
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
        
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        
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
                
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO dav_aspnetcore_server_resource_lock VALUES (@Id, @Uri, @LockType, @Owner, @Recursive, @Timeout, @Issued, @Depth)";
            command.Parameters.Add(new NpgsqlParameter("@Id", newLock.Id.AbsoluteUri));
            command.Parameters.Add(new NpgsqlParameter("@Uri", newLock.Uri.LocalPath));
            command.Parameters.Add(new NpgsqlParameter("@LockType", (int)newLock.LockType));
            command.Parameters.Add(new NpgsqlParameter("@Owner", newLock.Owner.ToString(SaveOptions.DisableFormatting)));
            command.Parameters.Add(new NpgsqlParameter("@Recursive", newLock.Recursive));
            command.Parameters.Add(new NpgsqlParameter("@Timeout", (int)newLock.Timeout.TotalSeconds));
            command.Parameters.Add(new NpgsqlParameter("@Issued", (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds));
            command.Parameters.Add(new NpgsqlParameter("@Depth", depth));
            
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
        
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP 1 Id FROM dav_aspnetcore_server_resource_lock WHERE Id = @Id AND Uri = @Uri AND (Issued + Timeout > @TotalSeconds OR Timeout = 0)";
        command.Parameters.Add(new NpgsqlParameter("@Id", token.AbsoluteUri));
        command.Parameters.Add(new NpgsqlParameter("@Uri", uri.LocalPath));
        command.Parameters.Add(new NpgsqlParameter("@TotalSeconds", (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds));
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetString("Id");
            await using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = "UPDATE dav_aspnetcore_server_resource_lock SET Timeout = @Timeout, Issued = @TotalSeconds WHERE Id = @Id";
            updateCommand.Parameters.Add(new NpgsqlParameter("@Id", id));
            updateCommand.Parameters.Add(new NpgsqlParameter("@Timeout", (int)timeout.TotalSeconds));
            updateCommand.Parameters.Add(new NpgsqlParameter("@TotalSeconds", (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds));

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
        
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TOP 1 Id FROM dav_aspnetcore_server_resource_lock WHERE Id = @Id AND Uri = @Uri AND (Issued + Timeout > @TotalSeconds OR Timeout = 0)";
        command.Parameters.Add(new NpgsqlParameter("@Id", token.AbsoluteUri));
        command.Parameters.Add(new NpgsqlParameter("@Uri", uri.LocalPath));
        command.Parameters.Add(new NpgsqlParameter("@TotalSeconds", (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds));
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetString("Id");
            await using var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM dav_aspnetcore_server_resource_lock WHERE Id = @Id";
            deleteCommand.Parameters.Add(new NpgsqlParameter("@Id", id));

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
        
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await RemoveStaleLocksAsync();

        var allActiveLocks = new List<ResourceLock>();
        var depth = uri.LocalPath.Count(x => x == '/') - 1;

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM dav_aspnetcore_server_resource_lock WHERE ((Depth <= @Depth AND Recursive = true) OR Uri = @Uri) AND (Issued + Timeout > @TotalSeconds OR Timeout = 0)";
        command.Parameters.Add(new NpgsqlParameter("@Depth", depth));
        command.Parameters.Add(new NpgsqlParameter("@Uri", uri.LocalPath));
        command.Parameters.Add(new NpgsqlParameter("@TotalSeconds", (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds));

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
        connection.Dispose();
    }
    
    private async ValueTask RemoveStaleLocksAsync()
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dav_aspnetcore_server_resource_lock WHERE (Issued + Timeout < @TotalSeconds AND Timeout <> 0)";
        command.Parameters.Add(new NpgsqlParameter("@TotalSeconds", (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds));
        
        await command.ExecuteNonQueryAsync();
    }
}