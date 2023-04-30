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
        
        var requestUri = Context.Request.Path.ToUri();
        var result = await LockManager.UnlockAsync(
            requestUri,
            WebDavHeaders.LockTokenUri,
            cancellationToken);
        
        Context.SetResult(result);
    }
}