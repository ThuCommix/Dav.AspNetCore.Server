namespace Dav.AspNetCore.Server.Properties.Converters;

public class Int64Converter : IConverter<long>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object ToXml(long value) => value.ToString();

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public long FromXml(object? value) => long.Parse((string)value);
}