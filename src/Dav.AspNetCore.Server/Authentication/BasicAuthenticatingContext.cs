using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Authentication;

/// <summary>
/// Initializes a new <see cref="BasicAuthenticatingContext"/> class.
/// </summary>
/// <param name="HttpContext">The http context.</param>
/// <param name="Options">The basic authentication options.</param>
/// <param name="Scheme">The authentication scheme.</param>
/// <param name="UserName">The user name.</param>
/// <param name="Password">The password.</param>
public record BasicAuthenticatingContext(
    HttpContext HttpContext,
    BasicAuthenticationSchemeOptions Options,
    AuthenticationScheme Scheme,
    string UserName,
    string Password)
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