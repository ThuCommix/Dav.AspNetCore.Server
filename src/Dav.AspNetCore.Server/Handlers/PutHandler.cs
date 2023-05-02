namespace Dav.AspNetCore.Server.Handlers;

internal class PutHandler : RequestHandler
{
    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task HandleRequestAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = Context.Request.Path.ToUri();
        var itemName = requestUri.GetRelativeUri(Collection.Uri).LocalPath.Trim('/');
        var result = await Collection.CreateItemAsync(itemName, cancellationToken);
        if (result.Item == null)
        {
            Context.SetResult(result.StatusCode);
            return;
        }
        
        await result.Item.WriteDataAsync(Context.Request.Body, cancellationToken);
    }
}