using Dav.AspNetCore.Server.Store;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server;

public interface IRequestHandler
{
    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="store">The resource store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task HandleRequestAsync(
        HttpContext context, 
        IStore store, 
        CancellationToken cancellationToken = default);
}