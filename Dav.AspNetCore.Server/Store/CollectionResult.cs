using System.Diagnostics.CodeAnalysis;

namespace Dav.AspNetCore.Server.Store;

/// <summary>
/// Initializes a new <see cref="CollectionResult"/> class.
/// </summary>
/// <param name="StatusCode">The status code.</param>
/// <param name="Collection">The store collection.</param>
public record CollectionResult(
    DavStatusCode StatusCode,
    IStoreCollection? Collection)
{
    /// <summary>
    /// Creates an errored result.
    /// </summary>
    /// <param name="statusCode">The status code.</param>
    /// <returns>The item result.</returns>
    public static CollectionResult Fail(DavStatusCode statusCode) => new(statusCode, null);
    
    /// <summary>
    /// Creates a succeeded result.
    /// </summary>
    /// <param name="collection">The store collection.</param>
    /// <returns>The item result.</returns>
    public static CollectionResult Created(IStoreCollection collection) => new(DavStatusCode.Created, collection);
    
    [MemberNotNullWhen(returnValue: true, nameof(Collection))]
    private bool Success => StatusCode is DavStatusCode.Created;
}