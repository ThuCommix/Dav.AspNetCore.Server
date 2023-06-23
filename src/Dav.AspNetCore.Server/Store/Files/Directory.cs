using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Store.Properties;

namespace Dav.AspNetCore.Server.Store.Files;

public class Directory : IStoreCollection
{
    private readonly FileStore store;
    private readonly DirectoryProperties properties;

    private static readonly XElement Collection = new(XmlNames.Collection);
    
    /// <summary>
    /// Initializes a new <see cref="Directory"/> class.
    /// </summary>
    /// <param name="store">The file store.</param>
    /// <param name="properties">The file properties.</param>
    public Directory(
        FileStore store,
        DirectoryProperties properties)
    {
        ArgumentNullException.ThrowIfNull(store, nameof(store));
        ArgumentNullException.ThrowIfNull(properties, nameof(properties));

        this.store = store;
        this.properties = properties;
    }

    /// <summary>
    /// Gets the resource path.
    /// </summary>
    public ResourcePath Path => properties.Path;

    /// <summary>
    /// Gets a readable stream async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The readable stream.</returns>
    public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken = default) 
        => Task.FromResult(Stream.Null);

    /// <summary>
    /// Sets the data from the given stream async.
    /// </summary>
    /// <param name="stream">T
    /// he stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    public Task<DavStatusCode> WriteDataAsync(
        Stream stream, 
        CancellationToken cancellationToken = default) 
        => Task.FromResult(DavStatusCode.Conflict);

    /// <summary>
    /// Copies the store item to the destination resource collection.
    /// </summary>
    /// <param name="destination">The destination.</param>
    /// <param name="name">The name.</param>
    /// <param name="overwrite">A value indicating whether the resource at the destination will be overwritten.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public async Task<ItemResult> CopyAsync(
        IStoreCollection destination,
        string name,
        bool overwrite, 
        CancellationToken cancellationToken = default)
    {
        var result = await destination.CreateCollectionAsync(name, cancellationToken);
        if (result.Collection != null)
            store.ItemCache[result.Collection.Path] = result.Collection;
        
        return new ItemResult(result.StatusCode, result.Collection);
    }

    /// <summary>
    /// Gets the store item async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store item or null.</returns>
    public Task<IStoreItem?> GetItemAsync(string name, CancellationToken cancellationToken = default) 
        => store.GetItemAsync(ResourcePath.Combine(properties.Path, name), cancellationToken);

    /// <summary>
    /// Gets the store items async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store items.</returns>
    public async Task<IReadOnlyCollection<IStoreItem>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        if (store.CollectionCache.TryGetValue(Path, out var cacheItems) && !store.DisableCaching)
            return cacheItems;
        
        var items = new List<IStoreItem>();

        var directoryPaths = await store.GetDirectoriesAsync(properties.Path, cancellationToken);
        foreach (var path in directoryPaths)
        {
            var directoryProperties = await store.GetDirectoryPropertiesAsync(path, cancellationToken);
            var collection = new Directory(store, directoryProperties);
            
            store.ItemCache[collection.Path] = collection;
            items.Add(collection);
        }

        var filePaths = await store.GetFilesAsync(properties.Path, cancellationToken);
        foreach (var path in filePaths)
        {
            var fileProperties = await store.GetFilePropertiesAsync(path, cancellationToken);
            var item = new File(store, fileProperties);

            store.ItemCache[item.Path] = item;
            items.Add(item);
        }

        store.CollectionCache[Path] = items;

        return items;
    }

    /// <summary>
    /// Creates a new store collection async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The collection result.</returns>
    public async Task<CollectionResult> CreateCollectionAsync(
        string name, 
        CancellationToken cancellationToken = default)
    {
        var item = await GetItemAsync(name, cancellationToken);
        if (item != null)
            return CollectionResult.Fail(DavStatusCode.NotAllowed);
        
        var path = ResourcePath.Combine(properties.Path, name);
        await store.CreateDirectoryAsync(path, cancellationToken);
        
        var directoryProperties = await store.GetDirectoryPropertiesAsync(path, cancellationToken);
        var collection = new Directory(store, directoryProperties);

        store.ItemCache[collection.Path] = collection;
        
        return CollectionResult.Created(collection);
    }

    public async Task<ItemResult> CreateItemAsync(
        string name, 
        CancellationToken cancellationToken = default)
    {
        var path = ResourcePath.Combine(properties.Path, name);
        await (await store.OpenFileStreamAsync(path, OpenFileMode.Write, cancellationToken)).DisposeAsync();
        
        var fileProperties = await store.GetFilePropertiesAsync(path, cancellationToken);
        var item = new File(store, fileProperties);

        store.ItemCache[item.Path] = item;
        
        return new ItemResult(DavStatusCode.Created, item);
    }

    /// <summary>
    /// Moves a store item async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="destination">The destination store collection.</param>
    /// <param name="destinationName">The destination name.</param>
    /// <param name="overwrite">A value indicating whether the resource at the destination will be overwritten.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The item result.</returns>
    public async Task<ItemResult> MoveItemAsync(
        string name, 
        IStoreCollection destination, 
        string destinationName, 
        bool overwrite,
        CancellationToken cancellationToken = default)
    {
        var item = await GetItemAsync(name, cancellationToken).ConfigureAwait(false);
        if (item == null)
            return ItemResult.Fail(DavStatusCode.NotFound);

        var result = await item.CopyAsync(destination, destinationName, overwrite, cancellationToken);
        if (result.Item != null) 
            await DeleteItemAsync(name, cancellationToken);

        return result;
    }

    /// <summary>
    /// Deletes a store item async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The web dav status.</returns>
    public async Task<DavStatusCode> DeleteItemAsync(
        string name, 
        CancellationToken cancellationToken = default)
    {
        var path = ResourcePath.Combine(properties.Path, name);

        try
        {
            if (await store.DirectoryExistsAsync(path, cancellationToken))
            {
                await store.DeleteDirectoryAsync(path, cancellationToken);
                store.ItemCache.Remove(path);
                
                return DavStatusCode.NoContent;
            }

            if (await store.FileExistsAsync(path, cancellationToken))
            {
                await store.DeleteFileAsync(path, cancellationToken);
                store.ItemCache.Remove(path);
                
                return DavStatusCode.NoContent;
            }
        }
        catch (UnauthorizedAccessException)
        {
            return DavStatusCode.Forbidden;
        }
        catch (IOException)
        {
            return DavStatusCode.InternalServerError;
        }
        
        return DavStatusCode.NoContent;
    }

    internal static void RegisterProperties()
    {
        Property.RegisterProperty<Directory>(
            XmlNames.CreationDate,
            read: (context, _) =>
            {
                context.SetResult(XmlConvert.ToString(((Directory)context.Item).properties.Created, XmlDateTimeSerializationMode.Utc)); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<Directory>(
            XmlNames.DisplayName,
            read: (context, _) =>
            {
                context.SetResult(((Directory)context.Item).properties.Name); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<Directory>(
            XmlNames.GetLastModified,
            read: (context, _) =>
            {
                context.SetResult(((Directory)context.Item).properties.LastModified.ToString("R")); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<Directory>(
            XmlNames.GetContentLanguage,
            read: (context, _) =>
            {
                context.SetResult(CultureInfo.CurrentCulture.TwoLetterISOLanguageName); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<Directory>(
            XmlNames.ResourceType,
            read: (context, _) =>
            {
                context.SetResult(Collection); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));

        Property.RegisterSupportedLockProperty<Directory>();
        Property.RegisterLockDiscoveryProperty<Directory>();
    }
}