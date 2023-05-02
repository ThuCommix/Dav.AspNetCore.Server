namespace Dav.AspNetCore.Server;

[AttributeUsage(AttributeTargets.Field)]
public class DavStatusCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new <see cref="DavStatusCode"/> class.
    /// </summary>
    /// <param name="displayName">The display name.</param>
    public DavStatusCodeAttribute(string displayName)
    {
        DisplayName = displayName;
    }
    
    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; }
}