using Microsoft.AspNetCore.Authentication;

namespace Dav.AspNetCore.Server.Authentication;

public class BasicAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets the basic authentication events.
    /// </summary>
    public new BasicAuthenticationEvents Events { get; } = new();
    
    /// <summary>
    /// Gets or sets the realm.
    /// </summary>
    public string? Realm { get; set; }
}