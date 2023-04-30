using System.Security.Claims;

namespace Dav.AspNetCore.Server.Authentication;

public class DigestAuthenticationEvents
{
    public delegate Task<string?> RequestPasswordDelegate(
        DigestAuthenticationContext context, 
        CancellationToken cancellationToken = default);
    
    public delegate Task AuthenticatedDelegate(
        DigestAuthenticationContext context,
        ClaimsIdentity claimsIdentity, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fired when the password is for a specific user is requested.
    /// </summary>
    public RequestPasswordDelegate? OnRequestPassword { get; set; }
    
    /// <summary>
    /// Fires when the user was authenticated.
    /// </summary>
    public AuthenticatedDelegate? OnAuthenticated { get; set; }
    
    
}