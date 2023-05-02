using System.Globalization;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Store.Properties;

namespace Dav.AspNetCore.Server.Store.Files;

public class Directory : IStoreCollection
{
    private readonly FileStore store;
    private readonly DirectoryProperties properties;

    private static readonly XElement Collection = new(XmlNames.Collection);

    private static readonly AttachedProperty CreationDateProperty = AttachedProperty.CreationDate<Directory>(
        getter: (_, item, _) => ValueTask.FromResult(PropertyResult.Success(item.properties.Created)),
        isComputed: true);

    private static readonly AttachedProperty DisplayNameProperty = AttachedProperty.DisplayName<Directory>(
        getter: (_, item, _) => ValueTask.FromResult(PropertyResult.Success(item.properties.Name)),
        isComputed: true);

    private static readonly AttachedProperty LastModifiedProperty = AttachedProperty.LastModified<Directory>(
        getter: (_, item, _) => ValueTask.FromResult(PropertyResult.Success(item.properties.LastModified)),
        isComputed: true);

    private static readonly AttachedProperty ContentLanguageProperty = AttachedProperty.ContentLanguage<Directory>(
        getter: (_, _, _) => ValueTask.FromResult(PropertyResult.Success(CultureInfo.CurrentCulture.TwoLetterISOLanguageName)),
        isComputed: true);

    private static readonly AttachedProperty ResourceTypeProperty = AttachedProperty.ResourceType<Directory>(
        getter: (_, _, _) => ValueTask.FromResult(PropertyResult.Success(Collection)),
        isComputed: true);

    private static readonly AttachedProperty SupportedLockProperty = AttachedProperty.SupportedLock<Directory>();
    private static readonly AttachedProperty LockDiscoveryProperty = AttachedProperty.LockDiscovery<Directory>();

    /// <summary>
    /// Initializes a new <see cref="Directory"/> class.
    /// </summary>
    /// <param name="store">The file store.</param>
    /// <param name="properties">The file properties.</param>
    /// <param name="lockManager">The lock manager.</param>
    public Directory(
        FileStore store,
        DirectoryProperties properties,
        ILockManager lockManager)
    {
        ArgumentNullException.ThrowIfNull(store, nameof(store));
        ArgumentNullException.ThrowIfNull(properties, nameof(properties));
        ArgumentNullException.ThrowIfNull(lockManager, nameof(lockManager));

        this.store = store;
        this.properties = properties;
        
        LockManager = lockManager;
        PropertyManager = new PropertyManager(this, new[]
        {
            CreationDateProperty,
            DisplayNameProperty,
            LastModifiedProperty,
            ContentLanguageProperty,
            ResourceTypeProperty,
            SupportedLockProperty,
            LockDiscoveryProperty
        });
    }

    /// <summary>
    /// Gets the uri.
    /// </summary>
    public Uri Uri => properties.Uri;

    /// <summary>
    /// Gets the property manager.
    /// </summary>
    public IPropertyManager PropertyManager { get; }

    /// <summary>
    /// Gets the lock manager.
    /// </summary>
    public ILockManager LockManager { get; }

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
            store.ItemCache[result.Collection.Uri] = result.Collection;
        
        return new ItemResult(result.StatusCode, result.Collection);
    }

    /// <summary>
    /// Gets the store item async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store item or null.</returns>
    public Task<IStoreItem?> GetItemAsync(string name, CancellationToken cancellationToken = default) 
        => store.GetItemAsync(UriHelper.Combine(properties.Uri, name), cancellationToken);

    /// <summary>
    /// Gets the store items async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store items.</returns>
    public async Task<IReadOnlyCollection<IStoreItem>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        if (store.CollectionCache.TryGetValue(Uri, out var cacheItems) && !store.DisableCaching)
            return cacheItems;
        
        var items = new List<IStoreItem>();

        var directoryUris = await store.GetDirectoriesAsync(properties.Uri, cancellationToken);
        foreach (var uri in directoryUris)
        {
            var directoryProperties = await store.GetDirectoryPropertiesAsync(uri, cancellationToken);
            var collection = new Directory(store, directoryProperties, LockManager);
            
            store.ItemCache[collection.Uri] = collection;
            items.Add(collection);
        }

        var fileUris = await store.GetFilesAsync(properties.Uri, cancellationToken);
        foreach (var uri in fileUris)
        {
            var fileProperties = await store.GetFilePropertiesAsync(uri, cancellationToken);
            var item = new File(store, fileProperties, LockManager);

            store.ItemCache[item.Uri] = item;
            items.Add(item);
        }

        store.CollectionCache[Uri] = items;

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
        
        var uri = UriHelper.Combine(properties.Uri, name);
        await store.CreateDirectoryAsync(uri, cancellationToken);
        
        var directoryProperties = await store.GetDirectoryPropertiesAsync(uri, cancellationToken);
        var collection = new Directory(store, directoryProperties, LockManager);

        store.ItemCache[collection.Uri] = collection;
        
        return CollectionResult.Created(collection);
    }

    public async Task<ItemResult> CreateItemAsync(
        string name, 
        CancellationToken cancellationToken = default)
    {
        var uri = UriHelper.Combine(properties.Uri, name);
        await (await store.OpenFileStreamAsync(uri, OpenFileMode.Write, cancellationToken)).DisposeAsync();
        
        var fileProperties = await store.GetFilePropertiesAsync(uri, cancellationToken);
        var item = new File(store, fileProperties, LockManager);

        store.ItemCache[item.Uri] = item;
        
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
        var uri = UriHelper.Combine(properties.Uri, name);

        try
        {
            if (await store.DirectoryExistsAsync(uri, cancellationToken))
            {
                await store.DeleteDirectoryAsync(uri, cancellationToken);
                store.ItemCache.Remove(uri);
                
                return DavStatusCode.NoContent;
            }

            if (await store.FileExistsAsync(uri, cancellationToken))
            {
                await store.DeleteFileAsync(uri, cancellationToken);
                store.ItemCache.Remove(uri);
                
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
}