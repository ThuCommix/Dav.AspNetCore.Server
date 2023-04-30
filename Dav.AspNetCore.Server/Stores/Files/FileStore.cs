using Dav.AspNetCore.Server.Locks;

namespace Dav.AspNetCore.Server.Stores.Files;

public abstract class FileStore : IStore
{
    private readonly ILockManager lockManager;

    /// <summary>
    /// Initializes a new <see cref="FileStore"/> class.
    /// </summary>
    /// <param name="lockManager">The lock manager.</param>
    protected FileStore(ILockManager lockManager)
    {
        ArgumentNullException.ThrowIfNull(lockManager, nameof(lockManager));
        
        this.lockManager = lockManager;
    }
    
    /// <summary>
    /// Gets the store item async.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store item or null.</returns>
    public async Task<IStoreItem?> GetItemAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (await DirectoryExistsAsync(uri, cancellationToken))
        {
            var directoryProperties = await GetDirectoryPropertiesAsync(uri, cancellationToken);
            return new Directory(this, directoryProperties, lockManager);
        }

        if (await FileExistsAsync(uri, cancellationToken))
        {
            var fileProperties = await GetFilePropertiesAsync(uri, cancellationToken);
            return new File(this, fileProperties, lockManager);
        }

        return null;
    }

    public abstract ValueTask<bool> DirectoryExistsAsync(Uri uri, CancellationToken cancellationToken = default);

    public abstract ValueTask<bool> FileExistsAsync(Uri uri, CancellationToken cancellationToken = default);

    public abstract ValueTask DeleteDirectoryAsync(Uri uri, CancellationToken cancellationToken = default);
    
    public abstract ValueTask DeleteFileAsync(Uri uri, CancellationToken cancellationToken = default);

    public abstract ValueTask<DirectoryProperties> GetDirectoryPropertiesAsync(Uri uri, CancellationToken cancellationToken = default);

    public abstract ValueTask<FileProperties> GetFilePropertiesAsync(Uri uri, CancellationToken cancellationToken = default);

    public abstract ValueTask<Stream> OpenFileStreamAsync(Uri uri, OpenFileMode mode, CancellationToken cancellationToken = default);

    public abstract ValueTask CreateDirectoryAsync(Uri uri, CancellationToken cancellationToken);

    public abstract ValueTask<Uri[]> GetFilesAsync(Uri uri, CancellationToken cancellationToken);
    
    public abstract ValueTask<Uri[]> GetDirectoriesAsync(Uri uri, CancellationToken cancellationToken);
}