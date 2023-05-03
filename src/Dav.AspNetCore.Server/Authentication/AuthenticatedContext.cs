using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Authentication;

public delegate Task AuthenticatedDelegate<TOptions>(
    AuthenticatedContext<TOptions> context,
    CancellationToken cancellationToken = default) where TOptions : AuthenticationSchemeOptions;

/// <summary>
/// Initializes a new <see cref="BasicAuthenticatingContext"/> class.
/// </summary>
/// <param name="HttpContext">The http context.</param>
/// <param name="Options">The basic authentication options.</param>
/// <param name="Scheme">The authentication scheme.</param>
/// <param name="ClaimsIdentity">The claims identity.</param>
public record AuthenticatedContext<TOptions>(
    HttpContext HttpContext,
    TOptions Options,
    AuthenticationScheme Scheme,
    ClaimsIdentity ClaimsIdentity) where TOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets the http request.
    /// </summary>
    public HttpRequest Request => HttpContext.Request;

    /// <summary>
    /// Gets the http response.
    /// </summary>
    public HttpResponse Response => HttpContext.Response;
}