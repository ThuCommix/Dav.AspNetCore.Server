namespace Dav.AspNetCore.Server.Store.Properties.Converters;

public class Int64Converter : IConverter<long>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object Convert(long value) => value.ToString();

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public long ConvertBack(object? value) => value == null ? 0L : long.Parse((string)value);
}