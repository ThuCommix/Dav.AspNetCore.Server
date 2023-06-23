using System.Runtime.InteropServices;

namespace Dav.AspNetCore.Server;

/// <summary>
/// Represents a web dav resource path.
/// </summary>
public class ResourcePath
{
    private readonly string value;
    private readonly Lazy<ResourcePath?> parentPath;

    /// <summary>
    /// Initializes a new <see cref="ResourcePath"/> class.
    /// </summary>
    /// <param name="value">The value.</param>
    public ResourcePath(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));

        if (!value.StartsWith('/'))
            throw new ArgumentException("A resource path must start with '/'.");

        this.value = Uri.UnescapeDataString(value);
        parentPath = new Lazy<ResourcePath?>(() => GetParentPath(this));
    }

    /// <summary>
    /// Gets the parent resource path.
    /// </summary>
    public ResourcePath? Parent => parentPath.Value;
    
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return this;
    }

    /// <summary>
    /// Converts the resource path into a file path.
    /// </summary>
    /// <returns></returns>
    public string ToFilePath()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? value.Replace('/', '\\') 
            : value;
    }
    
    /// <summary>
    /// Combines the resource path with a string.
    /// </summary>
    /// <param name="path1">The resource path.</param>
    /// <param name="path2">The string.</param>
    /// <returns>The combined resource path.</returns>
    public static ResourcePath Combine(ResourcePath path1, string path2)
    {
        if (!path2.StartsWith("/"))
            path2 = $"/{path2}";
        
        return Combine(path1, new ResourcePath(path2));
    }

    /// <summary>
    /// Combines two or more resource paths.
    /// </summary>
    /// <param name="paths">The resource paths.</param>
    /// <returns>The combined resource path.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static ResourcePath Combine(params ResourcePath[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths, nameof(paths));

        if (paths.Length < 2)
            throw new ArgumentException("There must be at least two paths to combine them.");
        
        var result = CombineInline(paths[0], paths[1]);
        for (var i = 2; i < paths.Length; i++)
        {
            result = CombineInline(result, paths[i]);
        }

        return result;

        ResourcePath CombineInline(ResourcePath path1, ResourcePath path2)
        {
            var path1Value = path1.value;
            if (!path1Value.EndsWith("/"))
                path1Value += "/";
            
            return new ResourcePath($"{path1Value}{path2.value.TrimStart('/')}");
        }
    }
    
    /// <summary>
    /// Gets the parent resource path.
    /// </summary>
    /// <param name="path">The resource path.</param>
    /// <returns>The parent resource path or null when the resource path has no parent.</returns>
    public static ResourcePath? GetParentPath(ResourcePath path)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        
        if (path.value == "/")
            return null;

        var pathParts = GetPathStringParts(path);
        return new ResourcePath($"{string.Join('/', pathParts.Take(pathParts.Length - 1))}/");
    }
    
    /// <summary>
    /// Gets the relative resource path.
    /// </summary>
    /// <param name="relativeTo">The base resource path.</param>
    /// <param name="path">The resource path.</param>
    /// <returns>The relative resource path.</returns>
    public static ResourcePath GetRelativePath(ResourcePath relativeTo, ResourcePath path)
    {
        ArgumentNullException.ThrowIfNull(relativeTo, nameof(relativeTo));
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        
        var relativeToParts = GetPathStringParts(relativeTo);
        var pathParts = GetPathStringParts(path);
        
        if (pathParts.Length > relativeToParts.Length)
            return path;
        
        // validate root
        for (var i = 0; i < pathParts.Length; i++)
        {
            if (relativeToParts[i].Trim('/') != pathParts[i].Trim('/'))
                return path;
        }

        var relativePath = string.Join(string.Empty, relativeToParts.Skip(pathParts.Length));
        if (!relativePath.StartsWith("/"))
            relativePath = $"/{relativePath}";

        if (relativePath.EndsWith("/"))
            relativePath = relativePath.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(relativePath))
            return new ResourcePath("/");
        
        return new ResourcePath(relativePath);
    }
    
    public static implicit operator string(ResourcePath path)
    {
        var parts = GetPathStringParts(path);
        var result = string.Join('/', parts.Select(Uri.EscapeDataString));
        return string.IsNullOrEmpty(result) ? "/" : result;
    }

    public static explicit operator ResourcePath(string value) => new(value);
    
    private static string[] GetPathStringParts(ResourcePath path)
    {
        var pathParts = path.value.EndsWith('/')
            ? path.value.Substring(0, path.value.Length - 1).Split('/')
            : path.value.Split('/');

        return pathParts;
    }
}