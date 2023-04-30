namespace Dav.AspNetCore.Server.Stores;

public static class StoreExtensions
{
    public static async Task<IStoreCollection?> GetCollectionAsync(
        this IStore store, 
        Uri uri, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store, nameof(store));

        var item = await store.GetItemAsync(uri, cancellationToken);
        return item as IStoreCollection;
    }
}