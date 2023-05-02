using System.Diagnostics.CodeAnalysis;

namespace Dav.AspNetCore.Server.Http.Headers;

public class OverwriteHeaderValue
{
    /// <summary>
    /// Initializes a new <see cref="OverwriteHeaderValue"/> class.
    /// </summary>
    /// <param name="overwrite">A value indicating whether the target resource should be overwritten.</param>
    public OverwriteHeaderValue(bool overwrite)
    {
        Overwrite = overwrite;
    }
    
    /// <summary>
    /// A value indicating whether the target resource should be overwritten.
    /// </summary>
    public bool Overwrite { get; }

    /// <summary>
    /// Parses the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The overwrite header value.</returns>
    /// <exception cref="FormatException"></exception>
    public static OverwriteHeaderValue Parse(string input)
    {
        if (TryParse(input, out var parsedValue))
            return parsedValue;

        throw new FormatException("The Overwrite-Header could not be parsed.");
    }

    /// <summary>
    /// Try to parse the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="parsedValue">The overwrite header value.</param>
    /// <returns>True on success, otherwise false.</returns>
    public static bool TryParse(string? input, [NotNullWhen(true)] out OverwriteHeaderValue? parsedValue)
    {
        parsedValue = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        switch (input.Trim())
        {
            case "T":
                parsedValue = new OverwriteHeaderValue(true);
                return true;
            case "F":
                parsedValue = new OverwriteHeaderValue(false);
                return true;
        }

        return false;
    }
}