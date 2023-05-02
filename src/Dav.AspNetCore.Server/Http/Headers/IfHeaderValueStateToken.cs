using System.Diagnostics;

namespace Dav.AspNetCore.Server.Http.Headers;

/// <summary>
/// Initializes a new <see cref="IfHeaderValueStateToken"/> class.
/// </summary>
/// <param name="Value">The state token value.</param>
/// <param name="Negate">A value indicating whether the condition is negated.</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public record IfHeaderValueStateToken(string Value, bool Negate)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Value;

    private string DebuggerDisplay => Negate ? $"Not {Value}" : Value;
}