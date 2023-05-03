namespace Dav.AspNetCore.Server.Authentication;

public class BasicAuthenticationEvents
{
    public delegate Task<bool> AuthenticatingDelegate(
        BasicAuthenticatingContext context, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked when the credentials have been extracted.
    /// </summary>
    public AuthenticatingDelegate? OnAuthenticating { get; set; }
    
    /// <summary>
    /// Invoked when the claims identity has been created.
    /// </summary>
    public AuthenticatedDelegate<BasicAuthenticationSchemeOptions>? OnAuthenticated { get; set; }
}