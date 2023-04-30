namespace Dav.AspNetCore.Server.Properties.Converters;

public class Bool32Converter : IConverter<bool>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object ToXml(bool value) => value ? "1" : "0";

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public bool FromXml(object value) => int.Parse((string)value) != 0;
}