using System.Diagnostics.CodeAnalysis;

namespace Dav.AspNetCore.Server.Stores;

/// <summary>
/// Initializes a new <see cref="ItemResult"/> class.
/// </summary>
/// <param name="StatusCode">The status code.</param>
/// <param name="Item">The item resource.</param>
public record ItemResult(
    DavStatusCode StatusCode,
    IStoreItem? Item)
{
    /// <summary>
    /// Creates an errored result.
    /// </summary>
    /// <param name="statusCode">The status code.</param>
    /// <returns>The item result.</returns>
    public static ItemResult Fail(DavStatusCode statusCode) => new(statusCode, null);
    
    /// <summary>
    /// Creates a succeeded result.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <returns>The item result.</returns>
    public static ItemResult Created(IStoreItem item) => new(DavStatusCode.Created, item);
    
    /// <summary>
    /// Creates a succeeded result.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <returns>The item result.</returns>
    public static ItemResult NoContent(IStoreItem item) => new(DavStatusCode.NoContent, item);
    
    [MemberNotNullWhen(returnValue: true, nameof(Item))]
    private bool Success => StatusCode is DavStatusCode.NoContent or DavStatusCode.Created;
}