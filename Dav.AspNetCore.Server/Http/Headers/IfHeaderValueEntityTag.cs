using System.Diagnostics;

namespace Dav.AspNetCore.Server.Http.Headers;

/// <summary>
/// Initializes a new <see cref="IfHeaderValueEntityTag"/> class.
/// </summary>
/// <param name="Value">The etag value.</param>
/// <param name="IsWeak">A value indicating whether the etag is weak.</param>
/// <param name="Negate">A value indicating whether the condition is negated.</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public record IfHeaderValueEntityTag(string Value, bool IsWeak, bool Negate)
{
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Value;

    private string DebuggerDisplay
    {
        get
        {
            var displayValue = IsWeak ? $"[W/\"{Value}\"]" : $"[\"{Value}\"]";
            return Negate ? $"Not {displayValue}" : displayValue;
        }
    }
}