using Microsoft.AspNetCore.Authentication;

namespace Dav.AspNetCore.Server.Authentication;

public class DigestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets the digest authentication events.
    /// </summary>
    public new DigestAuthenticationEvents Events { get; } = new();
    
    /// <summary>
    /// Gets or sets the realm.
    /// </summary>
    public string? Realm { get; set; }
}