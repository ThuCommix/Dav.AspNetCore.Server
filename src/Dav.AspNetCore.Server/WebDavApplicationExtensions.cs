using Microsoft.AspNetCore.Builder;

namespace Dav.AspNetCore.Server;

public static class WebDavApplicationExtensions
{
    /// <summary>
    /// Routes all requests to the web dav middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static IApplicationBuilder UseWebDav(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.UseMiddleware<WebDavMiddleware>();

        return app;
    }
}