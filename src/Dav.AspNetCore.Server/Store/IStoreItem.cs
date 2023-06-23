namespace Dav.AspNetCore.Server.Store;

public interface IStoreItem
{
    /// <summary>
    /// Gets the resource path.
    /// </summary>
    ResourcePath Path { get; }

    /// <summary>
    /// Gets a readable stream async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The readable stream.</returns>
    Task<Stream> GetReadableStreamAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the data from the given stream async.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    Task<DavStatusCode> WriteDataAsync(
        Stream stream, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies the store item to the destination store collection.
    /// </summary>
    /// <param name="destination">The destination.</param>
    /// <param name="name">The name.</param>
    /// <param name="overwrite">A value indicating whether the resource at the destination will be overwritten.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    Task<ItemResult> CopyAsync(
        IStoreCollection destination,
        string name,
        bool overwrite, 
        CancellationToken cancellationToken = default);
}