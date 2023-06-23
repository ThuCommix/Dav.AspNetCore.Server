namespace Dav.AspNetCore.Server.Handlers;

internal class UnlockHandler : RequestHandler
{
    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task HandleRequestAsync(CancellationToken cancellationToken = default)
    {
        if (WebDavHeaders.LockTokenUri == null)
        {
            Context.SetResult(DavStatusCode.BadRequest);
            return;
        }
        
        var requestPath = new ResourcePath(Context.Request.Path);
        var result = await LockManager.UnlockAsync(
            requestPath,
            WebDavHeaders.LockTokenUri,
            cancellationToken);
        
        Context.SetResult(result);
    }
}