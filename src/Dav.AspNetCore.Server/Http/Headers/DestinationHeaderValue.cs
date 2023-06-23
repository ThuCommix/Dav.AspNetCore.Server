using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Http.Headers;

public class DestinationHeaderValue
{
    /// <summary>
    /// Initializes a new <see cref="DestinationHeaderValue"/> class.
    /// </summary>
    /// <param name="destination">The destination.</param>
    public DestinationHeaderValue(ResourcePath destination)
    {
        ArgumentNullException.ThrowIfNull(destination, nameof(destination));
        Destination = destination;
    }
    
    /// <summary>
    /// Gets the destination resource path.
    /// </summary>
    public ResourcePath Destination { get; }
    
    /// <summary>
    /// Parses the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The destination header value.</returns>
    /// <exception cref="FormatException"></exception>
    public static DestinationHeaderValue Parse(string input)
    {
        if (TryParse(input, out var parsedValue))
            return parsedValue;

        throw new FormatException("The Destination-Header could not be parsed.");
    }

    /// <summary>
    /// Try to parse the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="parsedValue">The destination header value.</param>
    /// <returns>True on success, otherwise false.</returns>
    public static bool TryParse(string? input, [NotNullWhen(true)] out DestinationHeaderValue? parsedValue)
    {
        parsedValue = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            parsedValue = new DestinationHeaderValue(new ResourcePath(uri.LocalPath));
            return true;
        }

        if(!input.StartsWith("/") && Uri.TryCreate($"/{input}", UriKind.Absolute, out var uri2))
        {
            parsedValue = new DestinationHeaderValue(new ResourcePath(uri2.LocalPath));
            return true;
        }

        return false;
    }
}