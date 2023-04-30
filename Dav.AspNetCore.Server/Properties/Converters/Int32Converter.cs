namespace Dav.AspNetCore.Server.Properties.Converters;

public class Int32Converter : IConverter<int>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object ToXml(int value) => value.ToString();

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public int FromXml(object? value) => int.Parse((string)value);
}