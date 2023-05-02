using Dav.AspNetCore.Server.Http;
using Dav.AspNetCore.Server.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Dav.AspNetCore.Server.Handlers;

internal class OptionsHandler : IRequestHandler
{
    /// <summary>
    /// Handles the web dav request async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="store">The store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public Task HandleRequestAsync(
        HttpContext context,
        IStore store,
        CancellationToken cancellationToken = default)
    {
        context.Response.Headers["DAV"] = new[] { "1", "2" };
        context.Response.Headers["Allow"] = new StringValues(new[]
        {
            WebDavMethods.Options,
            WebDavMethods.Head,
            WebDavMethods.Get,
            WebDavMethods.Put,
            WebDavMethods.Delete,
            WebDavMethods.MkCol,
            WebDavMethods.PropFind,
            WebDavMethods.PropPatch,
            WebDavMethods.Move,
            WebDavMethods.Copy,
            WebDavMethods.Lock,
            WebDavMethods.Unlock
        });

        context.Response.StatusCode = StatusCodes.Status200OK;
        return Task.CompletedTask;
    }
}