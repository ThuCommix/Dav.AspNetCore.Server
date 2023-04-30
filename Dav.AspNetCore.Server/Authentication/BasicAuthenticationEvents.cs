using System.Security.Claims;

namespace Dav.AspNetCore.Server.Authentication;

public class BasicAuthenticationEvents
{
    public delegate Task<bool> AuthenticateDelegate(
        BasicAuthenticationContext context, 
        CancellationToken cancellationToken = default);
    
    public delegate Task AuthenticatedDelegate(
        BasicAuthenticationContext context, 
        ClaimsIdentity claimsIdentity, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fired when the authentication should take place.
    /// </summary>
    public AuthenticateDelegate? OnAuthenticate { get; set; }
    
    /// <summary>
    /// Fires when the user was authenticated.
    /// </summary>
    public AuthenticatedDelegate? OnAuthenticated { get; set; }
}