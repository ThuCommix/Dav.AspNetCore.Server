namespace Dav.AspNetCore.Server.Http.Headers;

public class WebDavRequestHeaders
{
    /// <summary>
    /// Gets the <see cref="IfHeaderValue"/>.
    /// </summary>
    public required IReadOnlyCollection<IfHeaderValueCondition> If { get; init; }

    /// <summary>
    /// Gets the <see cref="DepthHeaderValue"/>.
    /// </summary>
    public required Depth? Depth { get; init; }

    /// <summary>
    /// Gets the <see cref="DestinationHeaderValue"/>.
    /// </summary>
    public required Uri? Destination { get; init; }

    /// <summary>
    /// Gets the <see cref="OverwriteHeaderValue"/>.
    /// </summary>
    public required bool? Overwrite { get; init; }

    /// <summary>
    /// Gets the <see cref="LockTokenHeaderValue"/>.
    /// </summary>
    public required Uri? LockTokenUri { get; init; }

    /// <summary>
    /// Gets the <see cref="TimeoutHeaderValue"/>.
    /// </summary>
    public required IReadOnlyCollection<TimeSpan> Timeouts { get; init; }
}