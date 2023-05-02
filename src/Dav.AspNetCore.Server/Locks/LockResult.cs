using System.Diagnostics.CodeAnalysis;

namespace Dav.AspNetCore.Server.Locks;

/// <summary>
/// Initializes a new <see cref="LockResult"/> class.
/// </summary>
/// <param name="StatusCode">The status code.</param>
/// <param name="ResourceLock">The resource lock.</param>
public record LockResult(
    DavStatusCode StatusCode,
    ResourceLock? ResourceLock = null)
{
    [MemberNotNullWhen(returnValue: true, nameof(ResourceLock))]
    private bool Success => StatusCode == DavStatusCode.Ok;
}