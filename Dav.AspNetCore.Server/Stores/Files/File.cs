using System.Globalization;
using System.Security.Cryptography;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Properties;
using Microsoft.AspNetCore.StaticFiles;

namespace Dav.AspNetCore.Server.Stores.Files;

public class File : IStoreItem
{
    private readonly FileStore store;
    private readonly FileProperties properties;

    private static readonly IPropertyManager DefaultPropertyManager = new DefaultPropertyManager<File>(new Property<File>[]
    {
        DefaultProperty.CreationDate<File>(
            (_, item, _) => ValueTask.FromResult(PropertyResult.Success(item.properties.Created))),
        DefaultProperty.DisplayName<File>(
            (_, item, _) => ValueTask.FromResult(PropertyResult.Success(item.properties.Name))),
        DefaultProperty.LastModified<File>(
            (_, item, _) => ValueTask.FromResult(PropertyResult.Success(item.properties.LastModified))),
        DefaultProperty.ContentLength<File>(
            (_, item, _) => ValueTask.FromResult(PropertyResult.Success(item.properties.Length))),
        DefaultProperty.ContentType<File>(
            (_, item, _) => ValueTask.FromResult(PropertyResult.Success(GetMimeTypeForFileExtension(item.Uri)))),
        DefaultProperty.ContentLanguage<File>(
            (_, _, _) => ValueTask.FromResult(PropertyResult.Success(CultureInfo.CurrentCulture.TwoLetterISOLanguageName))),
        DefaultProperty.Etag<File>(
            async (_, item, cancellationToken) => PropertyResult.Success(await ComputeEtagAsync(item.store, item.properties.Uri, cancellationToken))),
        DefaultProperty.ResourceType<File>(
            (_, _, _) => ValueTask.FromResult(PropertyResult.Success<XElement?>(null))),
        DefaultProperty.SupportedLock<File>(),
        DefaultProperty.LockDiscovery<File>()
    });

    /// <summary>
    /// Initializes a new <see cref="File"/> class.
    /// </summary>
    /// <param name="store">The file store</param>
    /// <param name="properties">The file properties.</param>
    /// <param name="lockManager">The lock manager.</param>
    public File(
        FileStore store,
        FileProperties properties,
        ILockManager lockManager)
    {
        ArgumentNullException.ThrowIfNull(store, nameof(store));
        ArgumentNullException.ThrowIfNull(properties, nameof(properties));
        ArgumentNullException.ThrowIfNull(lockManager, nameof(lockManager));

        this.store = store;
        this.properties = properties;
        LockManager = lockManager;
    }

    /// <summary>
    /// Gets the uri.
    /// </summary>
    public Uri Uri => properties.Uri;

    /// <summary>
    /// Gets the property manager.
    /// </summary>
    public IPropertyManager PropertyManager => DefaultPropertyManager;

    /// <summary>
    /// Gets the lock manager.
    /// </summary>
    public ILockManager LockManager { get; }

    /// <summary>
    /// Gets a readable stream async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The readable stream.</returns>
    public async Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken = default)
        => await store.OpenFileStreamAsync(properties.Uri, OpenFileMode.Read, cancellationToken);

    /// <summary>
    /// Sets the data from the given stream async.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    public async Task<DavStatusCode> WriteDataAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        await using var fileStream = await store.OpenFileStreamAsync(properties.Uri, OpenFileMode.Write, cancellationToken);
        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

        return DavStatusCode.Ok;
    }

    /// <summary>
    /// Copies the store item to the destination store collection.
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
        var item = await destination.GetItemAsync(name, cancellationToken);
        if (item != null && !overwrite)
            return ItemResult.Fail(DavStatusCode.PreconditionFailed);

        DavStatusCode statusCode;
        if (item != null && overwrite)
        {
            statusCode = await destination.DeleteItemAsync(name, cancellationToken);
            if (statusCode != DavStatusCode.NoContent)
                return ItemResult.Fail(DavStatusCode.PreconditionFailed);
        }

        var result = await destination.CreateItemAsync(name, cancellationToken);
        if (result.StatusCode != DavStatusCode.Created)
            return ItemResult.Fail(result.StatusCode);
        
        await using var readableStream = await GetReadableStreamAsync(cancellationToken);
        statusCode = await result.Item.WriteDataAsync(readableStream, cancellationToken);
        if (statusCode != DavStatusCode.Ok)
            return ItemResult.Fail(statusCode);

        return item != null 
            ? ItemResult.NoContent(result.Item) 
            : ItemResult.Created(result.Item);
    }
    
    private static string GetMimeTypeForFileExtension(Uri uri)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(uri.AbsolutePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return contentType;
    }
    
    private static async Task<string> ComputeEtagAsync(
        FileStore store,
        Uri uri, 
        CancellationToken cancellationToken = default)
    {
        await using var fileStream = await store.OpenFileStreamAsync(uri, OpenFileMode.Read, cancellationToken);
        using var algorithm = MD5.Create();
        var hash = await algorithm.ComputeHashAsync(fileStream, cancellationToken);

        return string.Concat(Array.ConvertAll(hash, h => h.ToString("X2")));
    }
}