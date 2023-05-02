using System.Diagnostics.CodeAnalysis;

namespace Dav.AspNetCore.Server.Http.Headers;

public class DepthHeaderValue
{
    /// <summary>
    /// Initializes a new <see cref="DepthHeaderValue"/> class.
    /// </summary>
    /// <param name="depth">The depth.</param>
    public DepthHeaderValue(Depth depth)
    {
        Depth = depth;
    }
    
    /// <summary>
    /// Gets the depth.
    /// </summary>
    public Depth Depth { get; }
    
    /// <summary>
    /// Parses the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The depth header value.</returns>
    /// <exception cref="FormatException"></exception>
    public static DepthHeaderValue Parse(string input)
    {
        if (TryParse(input, out var parsedValue))
            return parsedValue;

        throw new FormatException("The Depth-Header could not be parsed.");
    }

    /// <summary>
    /// Try to parse the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="parsedValue">The depth header value.</param>
    /// <returns>True on success, otherwise false.</returns>
    public static bool TryParse(string? input, [NotNullWhen(true)] out DepthHeaderValue? parsedValue)
    {
        parsedValue = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        switch (input.Trim())
        {
            case "0":
                parsedValue = new DepthHeaderValue(Depth.None);
                return true;
            case "1":
                parsedValue = new DepthHeaderValue(Depth.One);
                return true;
            case "infinity":
                parsedValue = new DepthHeaderValue(Depth.Infinity);
                return true;
        }

        return false;
    }
}