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

        var collectionName = requestPath.Name;
        var result = await Collection.CreateCollectionAsync(collectionName, cancellationToken);
        Context.SetResult(result.StatusCode);
    }
}