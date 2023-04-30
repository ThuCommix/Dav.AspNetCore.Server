using System.Xml;

namespace Dav.AspNetCore.Server.Properties.Converters;

public class Iso8601DateConverter : IConverter<DateTime>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object ToXml(DateTime value) => XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc);

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public DateTime FromXml(object? value) => XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.Utc);
}