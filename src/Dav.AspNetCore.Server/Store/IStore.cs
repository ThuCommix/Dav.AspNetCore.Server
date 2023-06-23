namespace Dav.AspNetCore.Server.Store;

public interface IStore
{
    /// <summary>
    /// Gets the store item async.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store item or null.</returns>
    Task<IStoreItem?> GetItemAsync(
        ResourcePath path,
        CancellationToken cancellationToken = default);
}