namespace Dav.AspNetCore.Server.Authentication;

public class DigestAuthenticationEvents
{
    public delegate Task<string?> PasswordRequestedDelegate(
        DigestPasswordRequestedContext context, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoked when the user name was extracted, and the password is requested to make a hash compare.
    /// </summary>
    public PasswordRequestedDelegate? OnPasswordRequested { get; set; }
    
    /// <summary>
    /// Invoked when the claims identity has been created.
    /// </summary>
    public AuthenticatedDelegate<DigestAuthenticationSchemeOptions>? OnAuthenticated { get; set; }
    
    
}