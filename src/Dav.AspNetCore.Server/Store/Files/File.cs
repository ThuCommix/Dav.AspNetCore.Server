using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using Dav.AspNetCore.Server.Store.Properties;
using Microsoft.AspNetCore.StaticFiles;

namespace Dav.AspNetCore.Server.Store.Files;

public class File : IStoreItem
{
    private readonly FileStore store;
    private readonly FileProperties properties;
    
    /// <summary>
    /// Initializes a new <see cref="File"/> class.
    /// </summary>
    /// <param name="store">The file store</param>
    /// <param name="properties">The file properties.</param>
    public File(
        FileStore store,
        FileProperties properties)
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
    public async Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken = default)
        => await store.OpenFileStreamAsync(properties.Path, OpenFileMode.Read, cancellationToken);

    /// <summary>
    /// Sets the data from the given stream async.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    public async Task<DavStatusCode> WriteDataAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        await using var fileStream = await store.OpenFileStreamAsync(properties.Path, OpenFileMode.Write, cancellationToken);
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
        if (result.Item == null)
            return ItemResult.Fail(result.StatusCode);
        
        await using var readableStream = await GetReadableStreamAsync(cancellationToken);
        statusCode = await result.Item.WriteDataAsync(readableStream, cancellationToken);
        if (statusCode != DavStatusCode.Ok)
            return ItemResult.Fail(statusCode);

        store.ItemCache[result.Item.Path] = result.Item;
        
        return item != null 
            ? ItemResult.NoContent(result.Item) 
            : ItemResult.Created(result.Item);
    }
    
    private static string GetMimeTypeForFileExtension(ResourcePath path)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(path.ToFilePath(), out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return contentType;
    }
    
    private static async Task<string> ComputeEtagAsync(
        FileStore store,
        ResourcePath path, 
        CancellationToken cancellationToken = default)
    {
        await using var fileStream = await store.OpenFileStreamAsync(path, OpenFileMode.Read, cancellationToken);
        using var algorithm = MD5.Create();
        var hash = await algorithm.ComputeHashAsync(fileStream, cancellationToken);

        return string.Concat(Array.ConvertAll(hash, h => h.ToString("X2")));
    }
    
    internal static void RegisterProperties()
    {
        Property.RegisterProperty<File>(
            XmlNames.CreationDate,
            read: (context, _) =>
            {
                context.SetResult(XmlConvert.ToString(((File)context.Item).properties.Created, XmlDateTimeSerializationMode.Utc)); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<File>(
            XmlNames.DisplayName,
            read: (context, _) =>
            {
                context.SetResult(((File)context.Item).properties.Name); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<File>(
            XmlNames.GetLastModified,
            read: (context, _) =>
            {
                context.SetResult(((File)context.Item).properties.LastModified.ToString("R")); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<File>(
            XmlNames.GetContentLength,
            read: (context, _) =>
            {
                context.SetResult(((File)context.Item).properties.Length.ToString()); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<File>(
            XmlNames.GetContentType,
            read: (context, _) =>
            {
                context.SetResult(GetMimeTypeForFileExtension(context.Item.Path)); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<File>(
            XmlNames.GetContentLanguage,
            read: (context, _) =>
            {
                context.SetResult(CultureInfo.CurrentCulture.TwoLetterISOLanguageName); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));
        
        Property.RegisterProperty<File>(
            XmlNames.GetEtag,
            read: async (context, cancellationToken) =>
            {
                var fileItem = (File)context.Item;
                var etag = await ComputeEtagAsync(fileItem.store, fileItem.properties.Path, cancellationToken);
                
                context.SetResult(etag);
            },
            metadata: new PropertyMetadata(Expensive: true, Computed: true));
        
        Property.RegisterProperty<File>(
            XmlNames.ResourceType,
            read: (context, _) =>
            {
                context.SetResult(null); 
                return ValueTask.CompletedTask;
            },
            metadata: new PropertyMetadata(Computed: true));

        Property.RegisterSupportedLockProperty<File>();
        Property.RegisterLockDiscoveryProperty<File>();
    }
}