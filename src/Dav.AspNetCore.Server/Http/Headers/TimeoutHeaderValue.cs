using System.Diagnostics.CodeAnalysis;

namespace Dav.AspNetCore.Server.Http.Headers;

public class TimeoutHeaderValue
{
    /// <summary>
    /// Initializes a new <see cref="TimeoutHeaderValue"/> class.
    /// </summary>
    /// <param name="timeouts">The timeouts.</param>
    public TimeoutHeaderValue(params TimeSpan[] timeouts)
    {
        Timeouts = timeouts;
    }
    
    /// <summary>
    /// Gets the timeouts.
    /// </summary>
    public TimeSpan[] Timeouts { get; }

    /// <summary>
    /// Parses the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The timeout header value.</returns>
    /// <exception cref="FormatException"></exception>
    public static TimeoutHeaderValue Parse(string input)
    {
        if (TryParse(input, out var parsedValue))
            return parsedValue;

        throw new FormatException("The Timeout-Header could not be parsed.");
    }

    /// <summary>
    /// Try to parse the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="parsedValue">The timeout header value.</param>
    /// <returns>True on success, otherwise false.</returns>
    public static bool TryParse(string? input, [NotNullWhen(true)] out TimeoutHeaderValue? parsedValue)
    {
        parsedValue = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var timeouts = new List<TimeSpan>();
        var values = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var value in values)
        {
            if (value.Equals("Infinite", StringComparison.CurrentCultureIgnoreCase))
            {
                timeouts.Add(TimeSpan.Zero);
                continue;
            }

            if (value.StartsWith("Second-", StringComparison.InvariantCultureIgnoreCase))
            {
                if (long.TryParse(value.Replace("Second-", string.Empty, StringComparison.InvariantCultureIgnoreCase), out var seconds))
                {
                    timeouts.Add(TimeSpan.FromSeconds(seconds));
                }
            }
        }

        if (timeouts.Count > 0)
        {
            parsedValue = new TimeoutHeaderValue(timeouts.ToArray());
            return true;
        }

        return false;
    }
}