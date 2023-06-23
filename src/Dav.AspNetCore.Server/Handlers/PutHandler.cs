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
        var requestPath = new ResourcePath(Context.Request.Path);
        var itemName = ResourcePath.GetRelativePath(requestPath, Collection.Path).Name!;
        var result = await Collection.CreateItemAsync(itemName, cancellationToken);
        if (result.Item == null)
        {
            Context.SetResult(result.StatusCode);
            return;
        }
        
        await result.Item.WriteDataAsync(Context.Request.Body, cancellationToken);
    }
}