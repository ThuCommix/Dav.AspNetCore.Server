using Dav.AspNetCore.Server.Store;

namespace Dav.AspNetCore.Server.Tests;

public record TestStoreItem(ResourcePath Path) : IStoreItem
{
    public Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<DavStatusCode> WriteDataAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}