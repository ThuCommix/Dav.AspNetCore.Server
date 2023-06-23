namespace Dav.AspNetCore.Server.Handlers;

internal class MkColHandler : RequestHandler
{
    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task HandleRequestAsync(CancellationToken cancellationToken = default)
    {
        var requestPath = new ResourcePath(Context.Request.Path);
        var parentPath = requestPath.Parent ?? ResourcePath.Root;

        var collectionName = ResourcePath.GetRelativePath(requestPath, parentPath).Name!;
        var result = await Collection.CreateCollectionAsync(collectionName, cancellationToken);
        Context.SetResult(result.StatusCode);
    }
}