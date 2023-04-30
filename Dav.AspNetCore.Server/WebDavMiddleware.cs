using Dav.AspNetCore.Server.Http;
using Dav.AspNetCore.Server.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dav.AspNetCore.Server;

internal class WebDavMiddleware
{
    private readonly WebDavOptions webDavOptions;
    private readonly ILogger<WebDavMiddleware> logger;

    private static readonly string DefaultServerName = $"Dav.AspNetCore.Server/{typeof(WebDavMiddleware).Assembly.GetName().Version}";

    /// <summary>
    /// Initializes a new <see cref="WebDavMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next request delegate.</param>
    /// <param name="webDavOptions">The web dav options.</param>
    /// <param name="logger">The logger.</param>
    public WebDavMiddleware(
        RequestDelegate next,
        WebDavOptions webDavOptions,
        ILogger<WebDavMiddleware> logger)
    {
        this.webDavOptions = webDavOptions;
        this.logger = logger;
    }

    /// <summary>
    /// Invokes the middleware async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task InvokeAsync(HttpContext context)
    {
        var middlewareStart = DateTime.UtcNow;

        if (webDavOptions.RequiresAuthentication &&
            context.Request.Method != WebDavMethods.Options &&
            context.User.Identity?.IsAuthenticated != true)
        {
            await context.ChallengeAsync();
            return;
        }

        var resourceStore = context.RequestServices.GetService<IStore>();
        if (resourceStore == null)
            throw new InvalidOperationException("MapWebDav was used but it was never added. Use AddWebDav during service configuration.");
        
        if (!webDavOptions.DisableServerName)
            context.Response.Headers["Server"] = string.IsNullOrWhiteSpace(webDavOptions.ServerName)
                ? DefaultServerName
                : webDavOptions.ServerName;
        
        if (!RequestHandlerFactory.TryGetRequestHandler(context.Request.Method, out var handler))
        {
            logger.LogInformation($"Request {context.Request.Method} is not implemented.");
            context.Response.StatusCode = StatusCodes.Status501NotImplemented;
            return;
        }
        
        logger.LogInformation($"Request starting {context.Request.Method} {context.Request.Path}");

        try
        {
            await handler.HandleRequestAsync(context, resourceStore, context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Unexpected error while handling request {context.Request.Method} {context.Request.Path} {(DateTime.UtcNow - middlewareStart).TotalMilliseconds:F0}ms");
            
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
        
        logger.LogInformation($"Request finished {context.Request.Method} {context.Request.Path} {context.Response.StatusCode} {(DateTime.UtcNow - middlewareStart).TotalMilliseconds:F0}ms");
    }
}