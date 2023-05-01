namespace Dav.AspNetCore.Server.Store.Properties.Converters;

public class StringConverter : IConverter<string>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object Convert(string value) => value;

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public string ConvertBack(object? value) => value == null ? string.Empty : (string)value;
}