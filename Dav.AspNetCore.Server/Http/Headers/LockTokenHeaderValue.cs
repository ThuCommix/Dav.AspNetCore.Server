using System.Diagnostics.CodeAnalysis;

namespace Dav.AspNetCore.Server.Http.Headers;

public class LockTokenHeaderValue
{
    /// <summary>
    /// Initializes a new <see cref="LockTokenHeaderValue"/> class.
    /// </summary>
    /// <param name="uri">The uri.</param>
    public LockTokenHeaderValue(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri, nameof(uri));
        Uri = uri;
    }
    
    /// <summary>
    /// Gets the uri.
    /// </summary>
    public Uri Uri { get; }
    
    /// <summary>
    /// Parses the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The lock token header value.</returns>
    /// <exception cref="FormatException"></exception>
    public static LockTokenHeaderValue Parse(string input)
    {
        if (TryParse(input, out var parsedValue))
            return parsedValue;

        throw new FormatException("The Lock-Token-Header could not be parsed.");
    }

    /// <summary>
    /// Try to parse the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="parsedValue">The lock token header value.</param>
    /// <returns>True on success, otherwise false.</returns>
    public static bool TryParse(string? input, [NotNullWhen(true)] out LockTokenHeaderValue? parsedValue)
    {
        parsedValue = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (Uri.TryCreate(input.TrimStart('<').TrimEnd('>'), UriKind.RelativeOrAbsolute, out var uri))
        {
            parsedValue = new LockTokenHeaderValue(uri);
            return true;
        }

        return false;
    }
}