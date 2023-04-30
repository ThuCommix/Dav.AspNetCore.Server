using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Properties.Converters;

public class XElementConverter : IConverter<XElement?>
{
    /// <summary>
    /// Converts the given value to xml.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The converted value.</returns>
    public object? ToXml(XElement? value) => value;

    /// <summary>
    /// Converts the given xml value.
    /// </summary>
    /// <param name="value">The xml value.</param>
    /// <returns>The converted value.</returns>
    public XElement? FromXml(object? value) => (XElement?)value;
}