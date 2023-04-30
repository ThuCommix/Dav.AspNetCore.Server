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
        var requestUri = Context.Request.Path.ToUri();
        var parentUri = requestUri.GetParent();

        var collectionName = requestUri.GetRelativeUri(parentUri).LocalPath.Trim('/');
        var result = await Collection.CreateCollectionAsync(collectionName, cancellationToken);
        Context.SetResult(result.StatusCode);
    }
}