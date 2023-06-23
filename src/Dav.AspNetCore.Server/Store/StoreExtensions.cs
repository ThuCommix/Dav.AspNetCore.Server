namespace Dav.AspNetCore.Server.Store;

public static class StoreExtensions
{
    public static async Task<IStoreCollection?> GetCollectionAsync(
        this IStore store, 
        ResourcePath path, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store, nameof(store));
        ArgumentNullException.ThrowIfNull(path, nameof(path));

        var item = await store.GetItemAsync(path, cancellationToken);
        return item as IStoreCollection;
    }
}