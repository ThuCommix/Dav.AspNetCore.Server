using System.Globalization;

namespace Dav.AspNetCore.Server.Properties.Converters;

public class Rfc1123DateConverter : IConverter<DateTime>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object ToXml(DateTime value) => value.ToString("R");

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public DateTime FromXml(object? value) => DateTime.Parse((string)value, CultureInfo.InvariantCulture);
}