namespace Dav.AspNetCore.Server;

public class WebDavOptions
{
    /// <summary>
    /// Disallows propfind requests with depth set to infinity.
    /// </summary>
    public bool DisallowInfinityDepth { get; set; }
    
    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    public string? ServerName { get; set; }
    
    /// <summary>
    /// A value indicating whether the server header is disabled.
    /// </summary>
    public bool DisableServerName { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum lock timeout.
    /// </summary>
    public TimeSpan? MaxLockTimeout { get; set; } = null;
    
    /// <summary>
    /// A value indicating whether web dav requires authentication.
    /// </summary>
    public bool RequiresAuthentication { get; set; }
}