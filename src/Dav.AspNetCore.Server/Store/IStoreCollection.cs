namespace Dav.AspNetCore.Server.Store;

public interface IStoreCollection : IStoreItem
{
    /// <summary>
    /// Gets the store item async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store item or null.</returns>
    Task<IStoreItem?> GetItemAsync(
        string name, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the store items async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store items.</returns>
    Task<IReadOnlyCollection<IStoreItem>> GetItemsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new store collection async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The collection result.</returns>
    Task<CollectionResult> CreateCollectionAsync(
        string name, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new store item async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The item result.</returns>
    Task<ItemResult> CreateItemAsync(
        string name, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a store item async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="destination">The destination store collection.</param>
    /// <param name="destinationName">The destination name.</param>
    /// <param name="overwrite">A value indicating whether the resource at the destination will be overwritten.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The item result.</returns>
    Task<ItemResult> MoveItemAsync(
        string name, 
        IStoreCollection destination, 
        string destinationName,
        bool overwrite, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a store item async.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The web dav status.</returns>
    Task<DavStatusCode> DeleteItemAsync(
        string name, 
        CancellationToken cancellationToken = default);
}