namespace Dav.AspNetCore.Server.Store.Files;

public abstract class FileStore : IStore
{
    /// <summary>
    /// Gets the item cache.
    /// </summary>
    internal Dictionary<Uri, IStoreItem?> ItemCache { get; } = new();

    /// <summary>
    /// Gets the collection cache.
    /// </summary>
    internal Dictionary<Uri, List<IStoreItem>> CollectionCache { get; } = new();
    
    /// <summary>
    /// A value indicating whether caching will be disabled.
    /// </summary>
    public bool DisableCaching { get; set; }

    /// <summary>
    /// Gets the store item async.
    /// </summary>
    /// <param name="uri">The uri.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store item or null.</returns>
    public async Task<IStoreItem?> GetItemAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (ItemCache.TryGetValue(uri, out var cacheItem) && !DisableCaching)
            return cacheItem;
        
        if (await DirectoryExistsAsync(uri, cancellationToken))
        {
            var directoryProperties = await GetDirectoryPropertiesAsync(uri, cancellationToken);
            var directory = new Directory(this, directoryProperties);

            ItemCache[directory.Uri] = directory;
            return directory;
        }

        if (await FileExistsAsync(uri, cancellationToken))
        {
            var fileProperties = await GetFilePropertiesAsync(uri, cancellationToken);
            var file = new File(this, fileProperties);

            ItemCache[file.Uri] = file;
            return file;
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