namespace Dav.AspNetCore.Server.Store.Files;

public abstract class FileStore : IStore
{
    /// <summary>
    /// Gets the item cache.
    /// </summary>
    internal Dictionary<ResourcePath, IStoreItem?> ItemCache { get; } = new();

    /// <summary>
    /// Gets the collection cache.
    /// </summary>
    internal Dictionary<ResourcePath, List<IStoreItem>> CollectionCache { get; } = new();
    
    /// <summary>
    /// A value indicating whether caching will be disabled.
    /// </summary>
    public bool DisableCaching { get; set; }

    /// <summary>
    /// Gets the store item async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store item or null.</returns>
    public async Task<IStoreItem?> GetItemAsync(ResourcePath path, CancellationToken cancellationToken = default)
    {
        if (ItemCache.TryGetValue(path, out var cacheItem) && !DisableCaching)
            return cacheItem;
        
        if (await DirectoryExistsAsync(path, cancellationToken))
        {
            var directoryProperties = await GetDirectoryPropertiesAsync(path, cancellationToken);
            var directory = new Directory(this, directoryProperties);

            ItemCache[directory.Path] = directory;
            return directory;
        }

        if (await FileExistsAsync(path, cancellationToken))
        {
            var fileProperties = await GetFilePropertiesAsync(path, cancellationToken);
            var file = new File(this, fileProperties);

            ItemCache[file.Path] = file;
            return file;
        }

        return null;
    }

    public abstract ValueTask<bool> DirectoryExistsAsync(ResourcePath path, CancellationToken cancellationToken = default);

    public abstract ValueTask<bool> FileExistsAsync(ResourcePath path, CancellationToken cancellationToken = default);

    public abstract ValueTask DeleteDirectoryAsync(ResourcePath path, CancellationToken cancellationToken = default);
    
    public abstract ValueTask DeleteFileAsync(ResourcePath path, CancellationToken cancellationToken = default);

    public abstract ValueTask<DirectoryProperties> GetDirectoryPropertiesAsync(ResourcePath path, CancellationToken cancellationToken = default);

    public abstract ValueTask<FileProperties> GetFilePropertiesAsync(ResourcePath path, CancellationToken cancellationToken = default);

    public abstract ValueTask<Stream> OpenFileStreamAsync(ResourcePath path, OpenFileMode mode, CancellationToken cancellationToken = default);

    public abstract ValueTask CreateDirectoryAsync(ResourcePath path, CancellationToken cancellationToken);

    public abstract ValueTask<ResourcePath[]> GetFilesAsync(ResourcePath path, CancellationToken cancellationToken);
    
    public abstract ValueTask<ResourcePath[]> GetDirectoriesAsync(ResourcePath path, CancellationToken cancellationToken);
}