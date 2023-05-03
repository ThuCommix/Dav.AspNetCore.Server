using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Authentication;

/// <summary>
/// Initializes a new <see cref="DigestPasswordRequestedContext"/> clas.s.
/// </summary>
/// <param name="HttpContext">The http context.</param>
/// <param name="Options">The digest authentication options.</param>
/// <param name="Scheme">The authentication scheme.</param>
/// <param name="UserName">The user name.</param>
public record DigestPasswordRequestedContext(
    HttpContext HttpContext,
    DigestAuthenticationSchemeOptions Options,
    AuthenticationScheme Scheme,
    string UserName)
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