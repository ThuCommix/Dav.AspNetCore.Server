namespace Dav.AspNetCore.Server.Http.Headers;

/// <summary>
/// Initializes a new <see cref="IfHeaderValueCondition"/> class.
/// </summary>
/// <param name="Uri">The resource path.</param>
/// <param name="Tokens">The list of state tokens.</param>
/// <param name="Tags">The list of etags.</param>
public record IfHeaderValueCondition(
    Uri? Uri,
    IfHeaderValueStateToken[] Tokens,
    IfHeaderValueEntityTag[] Tags);