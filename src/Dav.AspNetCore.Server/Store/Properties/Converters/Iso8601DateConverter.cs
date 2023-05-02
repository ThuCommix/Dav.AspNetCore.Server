using System.Xml;

namespace Dav.AspNetCore.Server.Store.Properties.Converters;

public class Iso8601DateConverter : IConverter<DateTime>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object Convert(DateTime value) => XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc);

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public DateTime ConvertBack(object? value) => value == null ? DateTime.MinValue : XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.Utc);
}